using JobCardApp.Api.Data;
using JobCardApp.Api.Services;
using JobCardApp.Shared;
using JobCardApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCardApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private const string InvoiceActionRoles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Accounts)},{nameof(UserRole.Manager)}";

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly PdfService _pdf;
    private readonly EmailService _email;

    public InvoicesController(AppDbContext db, IConfiguration config, PdfService pdf, EmailService email)
    {
        _db = db;
        _config = config;
        _pdf = pdf;
        _email = email;
    }

    [HttpGet]
    public async Task<ActionResult<List<Invoice>>> GetAll([FromQuery] InvoiceStatus? status)
    {
        var query = _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Company)
            .Include(i => i.Lines)
            .Include(i => i.Allocations)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        return await query.OrderByDescending(i => i.IssuedOn).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Invoice>> Get(int id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Company)
            .Include(i => i.Lines)
            .Include(i => i.Allocations)
            .FirstOrDefaultAsync(i => i.Id == id);

        return invoice is null ? NotFound() : invoice;
    }

    [HttpPost("from-jobcard/{jobCardId:int}")]
    public async Task<ActionResult<Invoice>> CreateFromJobCard(int jobCardId)
    {
        var jobCard = await _db.JobCards
            .Include(j => j.Lines)
            .Include(j => j.Company)
            .FirstOrDefaultAsync(j => j.Id == jobCardId);
        if (jobCard is null) return NotFound();

        if (jobCard.Status != JobCardStatus.Completed)
            return BadRequest("Job card must be completed (POST /api/JobCards/{id}/complete) before it can be invoiced.");

        if (jobCard.Lines.Count == 0)
            return BadRequest("Job card has no line items to invoice.");

        if (await _db.Invoices.AnyAsync(i => i.JobCardId == jobCardId && i.Status != InvoiceStatus.Cancelled))
            return Conflict("This job card has already been invoiced.");

        var taxRate = _config.GetValue("Billing:TaxRate", 0.15m);
        var terms = _config.GetValue("Billing:PaymentTermDays", 30);
        var number = InvoiceFactory.NextReference("INV", await NextSequenceAsync());

        var invoice = InvoiceFactory.FromJobCard(jobCard, number, taxRate, terms);

        _db.Invoices.Add(invoice);
        jobCard.Status = JobCardStatus.Invoiced;
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = invoice.Id }, invoice);
    }

    [HttpPost]
    public async Task<ActionResult<Invoice>> Create(Invoice invoice)
    {
        invoice.Id = 0;
        invoice.Customer = null;
        invoice.JobCard = null;
        invoice.Company = null;
        foreach (var line in invoice.Lines) line.Id = 0;

        if (string.IsNullOrWhiteSpace(invoice.Number))
            invoice.Number = InvoiceFactory.NextReference("INV", await NextSequenceAsync());

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = invoice.Id }, invoice);
    }

    /// <summary>
    /// Draft-only edit of an invoice's lines/notes/due date. Once an invoice
    /// is Sent (or Overdue) its lines are locked — POST /api/invoices/{id}/revise
    /// must be used to bring it back to Draft before it can be edited again.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = InvoiceActionRoles)]
    public async Task<ActionResult<Invoice>> Update(int id, Invoice update)
    {
        var invoice = await _db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null) return NotFound();

        if (invoice.Status != InvoiceStatus.Draft)
            return Conflict("Only draft invoices can be edited — use POST /api/invoices/{id}/revise first.");

        invoice.DueOn = update.DueOn;
        invoice.Notes = update.Notes;

        _db.InvoiceLines.RemoveRange(invoice.Lines);
        invoice.Lines = update.Lines.Select(l => new InvoiceLine
        {
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice
        }).ToList();

        await _db.SaveChangesAsync();

        return await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Company)
            .Include(i => i.Lines)
            .Include(i => i.Allocations)
            .FirstAsync(i => i.Id == id);
    }

    /// <summary>
    /// Ordinary status transitions (Overdue, Cancelled, back to Draft).
    /// Paid/PartiallyPaid are NOT settable here — they're computed from real
    /// payment allocations (see PaymentsController) per §14: "An invoice
    /// should become Paid only when the allocated payment amount covers the
    /// invoice balance." Sent is also not available here — sending is a real
    /// email with a PDF attached, not a bare status flip; use
    /// POST /api/invoices/{id}/send instead.
    /// </summary>
    [HttpPost("{id:int}/status/{status}")]
    [Authorize(Roles = InvoiceActionRoles)]
    public async Task<IActionResult> SetStatus(int id, InvoiceStatus status)
    {
        if (status is InvoiceStatus.Paid or InvoiceStatus.PartiallyPaid)
            return BadRequest("Paid/PartiallyPaid are computed from payment allocations — use the Payments endpoints instead.");

        if (status == InvoiceStatus.Sent)
            return BadRequest("Use POST /api/invoices/{id}/send instead — sending emails the customer a PDF of the invoice.");

        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice is null) return NotFound();

        invoice.Status = status;
        invoice.PaidOn = null;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// The real "send" action (§ decision): renders a PDF of the invoice and
    /// emails it to the customer on file, then records SentAt/SentTo and
    /// moves the invoice from Draft to Sent. Only valid from Draft — an
    /// invoice that was already sent must be revised back to Draft first
    /// (this is the resend path, there is no separate "resend while still Sent").
    /// </summary>
    [HttpPost("{id:int}/send")]
    [Authorize(Roles = InvoiceActionRoles)]
    public async Task<ActionResult<Invoice>> Send(int id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Company)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null) return NotFound();

        if (!invoice.CanSend)
            return BadRequest($"A {invoice.Status} invoice cannot be sent — only draft invoices can be sent.");

        if (string.IsNullOrWhiteSpace(invoice.Customer?.Email))
            return BadRequest("This customer has no email address on file — add one before sending.");

        var pdfBytes = _pdf.RenderInvoice(invoice);
        await _email.SendWithAttachmentAsync(
            toEmail: invoice.Customer.Email!,
            toName: invoice.Customer.Name,
            subject: $"Invoice {invoice.Number}",
            bodyText: $"Hi {invoice.Customer.Name},\n\nPlease find attached invoice {invoice.Number}, total {invoice.Total:C}, due {invoice.DueOn:d}.\n\nRegards,\n{invoice.Company?.Name}",
            attachmentFileName: $"{invoice.Number}.pdf",
            attachmentBytes: pdfBytes);

        invoice.Status = InvoiceStatus.Sent;
        invoice.SentAt = DateTime.UtcNow;
        invoice.SentTo = invoice.Customer.Email;
        await _db.SaveChangesAsync();

        return invoice;
    }

    /// <summary>
    /// Reverts a Sent (or Overdue) invoice back to Draft so its lines become
    /// editable again. Paid/PartiallyPaid/Cancelled are final and NOT
    /// revisable (§ decision).
    /// </summary>
    [HttpPost("{id:int}/revise")]
    [Authorize(Roles = InvoiceActionRoles)]
    public async Task<ActionResult<Invoice>> Revise(int id)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice is null) return NotFound();

        if (!invoice.CanRevise)
            return BadRequest($"A {invoice.Status} invoice cannot be revised.");

        invoice.Status = InvoiceStatus.Draft;
        await _db.SaveChangesAsync();

        return await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Company)
            .Include(i => i.Lines)
            .Include(i => i.Allocations)
            .FirstAsync(i => i.Id == id);
    }

    private async Task<int> NextSequenceAsync()
        => await _db.Invoices.CountAsync(i => i.IssuedOn.Year == DateTime.UtcNow.Year) + 1;
}
