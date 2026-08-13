using JobCardApp.Api.Data;
using JobCardApp.Shared;
using JobCardApp.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCardApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
            .Include(i => i.Lines)
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
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id);

        return invoice is null ? NotFound() : invoice;
    }

    [HttpPost("from-jobcard/{jobCardId:int}")]
    public async Task<ActionResult<Invoice>> CreateFromJobCard(int jobCardId)
    {
        var jobCard = await _db.JobCards
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == jobCardId);
        if (jobCard is null) return NotFound();

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
        foreach (var line in invoice.Lines) line.Id = 0;

        if (string.IsNullOrWhiteSpace(invoice.Number))
            invoice.Number = InvoiceFactory.NextReference("INV", await NextSequenceAsync());

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = invoice.Id }, invoice);
    }

    [HttpPost("{id:int}/status/{status}")]
    public async Task<IActionResult> SetStatus(int id, InvoiceStatus status)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice is null) return NotFound();

        invoice.Status = status;
        invoice.PaidOn = status == InvoiceStatus.Paid ? DateTime.UtcNow : null;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<int> NextSequenceAsync()
        => await _db.Invoices.CountAsync(i => i.IssuedOn.Year == DateTime.UtcNow.Year) + 1;
}
