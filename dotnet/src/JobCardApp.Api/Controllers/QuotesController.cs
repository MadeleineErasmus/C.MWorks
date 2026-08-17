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
public class QuotesController : ControllerBase
{
    private const string QuoteActionRoles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Office)},{nameof(UserRole.Manager)}";

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly PdfService _pdf;
    private readonly EmailService _email;

    public QuotesController(AppDbContext db, IConfiguration config, PdfService pdf, EmailService email)
    {
        _db = db;
        _config = config;
        _pdf = pdf;
        _email = email;
    }

    [HttpGet]
    public async Task<ActionResult<List<Quote>>> GetAll([FromQuery] QuoteStatus? status)
    {
        var query = _db.Quotes
            .Include(q => q.Customer)
            .Include(q => q.Company)
            .Include(q => q.Lines)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(q => q.Status == status.Value);

        return await query.OrderByDescending(q => q.IssuedOn).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Quote>> Get(int id)
    {
        var quote = await _db.Quotes
            .Include(q => q.Customer)
            .Include(q => q.Company)
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id);

        return quote is null ? NotFound() : quote;
    }

    /// <summary>
    /// Quotes are not gated on job card completion — unlike invoicing, a
    /// quote typically precedes the work, not follows it.
    /// </summary>
    [HttpPost("from-jobcard/{jobCardId:int}")]
    public async Task<ActionResult<Quote>> CreateFromJobCard(int jobCardId)
    {
        var jobCard = await _db.JobCards
            .Include(j => j.Lines)
            .Include(j => j.Company)
            .FirstOrDefaultAsync(j => j.Id == jobCardId);
        if (jobCard is null) return NotFound();

        if (jobCard.Status == JobCardStatus.Cancelled)
            return BadRequest("Cannot quote a cancelled job card.");

        if (jobCard.Lines.Count == 0)
            return BadRequest("Job card has no line items to quote.");

        var taxRate = _config.GetValue("Billing:TaxRate", 0.15m);
        var validDays = _config.GetValue("Billing:QuoteValidDays", 30);
        var number = InvoiceFactory.NextReference("QUO", await NextSequenceAsync());

        var quote = QuoteFactory.FromJobCard(jobCard, number, taxRate, validDays);

        _db.Quotes.Add(quote);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = quote.Id }, quote);
    }

    [HttpPost]
    public async Task<ActionResult<Quote>> Create(Quote quote)
    {
        quote.Id = 0;
        quote.Customer = null;
        quote.JobCard = null;
        quote.Company = null;
        quote.Status = QuoteStatus.Draft;
        foreach (var line in quote.Lines) line.Id = 0;

        if (string.IsNullOrWhiteSpace(quote.Number))
            quote.Number = InvoiceFactory.NextReference("QUO", await NextSequenceAsync());

        _db.Quotes.Add(quote);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = quote.Id }, quote);
    }

    /// <summary>
    /// Draft-only edit of a quote's lines/notes/expiry. Once a quote is Sent
    /// its lines are locked — POST /api/quotes/{id}/revise must be used to
    /// bring it back to Draft before it can be edited again.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = QuoteActionRoles)]
    public async Task<ActionResult<Quote>> Update(int id, Quote update)
    {
        var quote = await _db.Quotes.Include(q => q.Lines).FirstOrDefaultAsync(q => q.Id == id);
        if (quote is null) return NotFound();

        if (quote.Status != QuoteStatus.Draft)
            return Conflict("Only draft quotes can be edited — use POST /api/quotes/{id}/revise first.");

        quote.ExpiresOn = update.ExpiresOn;
        quote.Notes = update.Notes;

        _db.QuoteLines.RemoveRange(quote.Lines);
        quote.Lines = update.Lines.Select(l => new QuoteLine
        {
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice
        }).ToList();

        await _db.SaveChangesAsync();

        return await _db.Quotes
            .Include(q => q.Customer)
            .Include(q => q.Company)
            .Include(q => q.Lines)
            .FirstAsync(q => q.Id == id);
    }

    /// <summary>
    /// Ordinary status transitions (Accepted, Rejected, Expired, Cancelled).
    /// Converting to an invoice is deliberately NOT available here — that's a
    /// separate operation with real side effects (§9: "a quote should not
    /// become an invoice merely because it was emailed"). Sent is also not
    /// available here — sending is a real email with a PDF attached, not a
    /// bare status flip; use POST /api/quotes/{id}/send instead.
    /// </summary>
    [HttpPost("{id:int}/status/{status}")]
    public async Task<IActionResult> SetStatus(int id, QuoteStatus status)
    {
        if (status == QuoteStatus.ConvertedToInvoice)
            return BadRequest("Use POST /api/quotes/{id}/convert-to-invoice instead.");

        if (status == QuoteStatus.Sent)
            return BadRequest("Use POST /api/quotes/{id}/send instead — sending emails the customer a PDF of the quote.");

        var quote = await _db.Quotes.FindAsync(id);
        if (quote is null) return NotFound();

        if (quote.Status == QuoteStatus.ConvertedToInvoice)
            return Conflict("This quote has already been converted to an invoice.");

        quote.Status = status;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// The real "send" action (§ decision): renders a PDF of the quote and
    /// emails it to the customer on file, then records SentAt/SentTo and
    /// moves the quote from Draft to Sent. Only valid from Draft — a quote
    /// that was already sent must be revised back to Draft first (this is
    /// the resend path, there is no separate "resend while still Sent").
    /// </summary>
    [HttpPost("{id:int}/send")]
    [Authorize(Roles = QuoteActionRoles)]
    public async Task<ActionResult<Quote>> Send(int id)
    {
        var quote = await _db.Quotes
            .Include(q => q.Customer)
            .Include(q => q.Company)
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quote is null) return NotFound();

        if (!quote.CanSend)
            return BadRequest($"A {quote.Status} quote cannot be sent — only draft quotes can be sent.");

        var additionalEmails = await _db.CustomerEmails
            .Where(e => e.CustomerId == quote.CustomerId)
            .Select(e => e.Email)
            .ToListAsync();

        var recipients = new[] { quote.Customer.Email }
            .Concat(additionalEmails)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
            return BadRequest("This customer has no email address on file — add one before sending.");

        var pdfBytes = _pdf.RenderQuote(quote);
        await _email.SendWithAttachmentAsync(
            toEmails: recipients,
            subject: $"Quote {quote.Number}",
            bodyText: $"Hi {quote.Customer.Name},\n\nPlease find attached quote {quote.Number}, total {quote.Total:C}.\n\nRegards,\n{quote.Company?.Name}",
            attachmentFileName: $"{quote.Number}.pdf",
            attachmentBytes: pdfBytes);

        quote.Status = QuoteStatus.Sent;
        quote.SentAt = DateTime.UtcNow;
        quote.SentTo = string.Join(", ", recipients);
        await _db.SaveChangesAsync();

        return quote;
    }

    /// <summary>
    /// Reverts a Sent quote back to Draft so its lines become editable again.
    /// Only valid from Sent — final states (Accepted/Rejected/Expired/
    /// ConvertedToInvoice/Cancelled) are not revisable (§ decision).
    /// </summary>
    [HttpPost("{id:int}/revise")]
    [Authorize(Roles = QuoteActionRoles)]
    public async Task<ActionResult<Quote>> Revise(int id)
    {
        var quote = await _db.Quotes.FindAsync(id);
        if (quote is null) return NotFound();

        if (!quote.CanRevise)
            return BadRequest($"A {quote.Status} quote cannot be revised.");

        quote.Status = QuoteStatus.Draft;
        await _db.SaveChangesAsync();

        return await _db.Quotes
            .Include(q => q.Customer)
            .Include(q => q.Company)
            .Include(q => q.Lines)
            .FirstAsync(q => q.Id == id);
    }

    [HttpPost("{id:int}/convert-to-invoice")]
    [Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Office)},{nameof(UserRole.Manager)}")]
    public async Task<ActionResult<Invoice>> ConvertToInvoice(int id)
    {
        var quote = await _db.Quotes
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quote is null) return NotFound();

        if (quote.Status is QuoteStatus.ConvertedToInvoice or QuoteStatus.Rejected
            or QuoteStatus.Expired or QuoteStatus.Cancelled)
            return Conflict($"A {quote.Status} quote cannot be converted to an invoice.");

        var terms = _config.GetValue("Billing:PaymentTermDays", 30);
        var invoiceNumber = InvoiceFactory.NextReference("INV", await NextInvoiceSequenceAsync());

        var invoice = QuoteFactory.ToInvoice(quote, invoiceNumber, terms);

        _db.Invoices.Add(invoice);
        quote.Status = QuoteStatus.ConvertedToInvoice;
        await _db.SaveChangesAsync();

        return CreatedAtAction("Get", "Invoices", new { id = invoice.Id }, invoice);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Office)},{nameof(UserRole.Manager)}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.Quotes.FindAsync(id);
        if (existing is null) return NotFound();

        if (existing.Status != QuoteStatus.Draft)
            return Conflict("Only draft quotes can be deleted — cancel it instead.");

        _db.Quotes.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<int> NextSequenceAsync()
        => await _db.Quotes.CountAsync(q => q.IssuedOn.Year == DateTime.UtcNow.Year) + 1;

    private async Task<int> NextInvoiceSequenceAsync()
        => await _db.Invoices.CountAsync(i => i.IssuedOn.Year == DateTime.UtcNow.Year) + 1;
}
