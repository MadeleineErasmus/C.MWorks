using JobCardApp.Api.Data;
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
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public InvoicesController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
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
    /// Ordinary status transitions (Sent, Overdue, Cancelled, back to Draft).
    /// Paid/PartiallyPaid are NOT settable here — they're computed from real
    /// payment allocations (see PaymentsController) per §14: "An invoice
    /// should become Paid only when the allocated payment amount covers the
    /// invoice balance."
    /// </summary>
    [HttpPost("{id:int}/status/{status}")]
    [Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Accounts)},{nameof(UserRole.Manager)}")]
    public async Task<IActionResult> SetStatus(int id, InvoiceStatus status)
    {
        if (status is InvoiceStatus.Paid or InvoiceStatus.PartiallyPaid)
            return BadRequest("Paid/PartiallyPaid are computed from payment allocations — use the Payments endpoints instead.");

        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice is null) return NotFound();

        invoice.Status = status;
        invoice.PaidOn = null;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<int> NextSequenceAsync()
        => await _db.Invoices.CountAsync(i => i.IssuedOn.Year == DateTime.UtcNow.Year) + 1;
}
