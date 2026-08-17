using JobCardApp.Api.Data;
using JobCardApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCardApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    public CustomersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Customer>>> GetAll([FromQuery] string? search)
    {
        var query = _db.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                EF.Functions.Like(c.Name, $"%{term}%") ||
                (c.ContactPerson != null && EF.Functions.Like(c.ContactPerson, $"%{term}%")) ||
                (c.Email != null && EF.Functions.Like(c.Email, $"%{term}%")) ||
                (c.Phone != null && EF.Functions.Like(c.Phone, $"%{term}%")) ||
                (c.VatNumber != null && EF.Functions.Like(c.VatNumber, $"%{term}%")));
        }

        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> Get(int id)
        => await _db.Customers.FindAsync(id) is { } c ? c : NotFound();

    /// <summary>
    /// What this customer has been charged before, newest first — for
    /// deciding a price on a new line, not for automatically setting one
    /// (§7: the user must explicitly choose to reuse a previous price).
    /// </summary>
    [HttpGet("{id:int}/pricing-history")]
    public async Task<ActionResult<List<PricingHistoryEntry>>> GetPricingHistory(int id, [FromQuery] string? search)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == id))
            return NotFound();

        var query =
            from line in _db.InvoiceLines
            join invoice in _db.Invoices on line.InvoiceId equals invoice.Id
            where invoice.CustomerId == id
            select new { line, invoice };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.Like(x.line.Description, $"%{term}%"));
        }

        return await query
            .OrderByDescending(x => x.invoice.IssuedOn)
            .Take(25)
            .Select(x => new PricingHistoryEntry
            {
                Description = x.line.Description,
                Quantity = x.line.Quantity,
                UnitPrice = x.line.UnitPrice,
                InvoiceNumber = x.invoice.Number,
                InvoiceDate = x.invoice.IssuedOn
            })
            .ToListAsync();
    }

    /// <summary>
    /// Running-balance statement of issued invoices (debits) and received
    /// payments (credits) for this customer — §16/§52 Priority 5. Draft and
    /// Cancelled invoices are excluded: a draft was never actually issued,
    /// and a cancelled one was reversed out.
    /// </summary>
    [HttpGet("{id:int}/statement")]
    public async Task<ActionResult<CustomerStatement>> GetStatement(
        int id, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        var from = fromDate ?? DateTime.MinValue;
        var to = toDate ?? DateTime.UtcNow;

        var invoices = await _db.Invoices
            .Where(i => i.CustomerId == id && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Cancelled)
            .Include(i => i.Lines)
            .ToListAsync();

        var payments = await _db.Payments
            .Where(p => p.CustomerId == id)
            .ToListAsync();

        var openingBalance = Math.Round(
            invoices.Where(i => i.IssuedOn < from).Sum(i => i.Total)
            - payments.Where(p => p.PaymentDate < from).Sum(p => p.Amount), 2);

        var transactions = invoices
            .Where(i => i.IssuedOn >= from && i.IssuedOn <= to)
            .Select(i => (Date: i.IssuedOn, Document: i.Number, Debit: i.Total, Credit: 0m))
            .Concat(payments
                .Where(p => p.PaymentDate >= from && p.PaymentDate <= to)
                .Select(p => (Date: p.PaymentDate,
                               Document: string.IsNullOrWhiteSpace(p.Reference) ? "Payment" : $"Payment ({p.Reference})",
                               Debit: 0m, Credit: p.Amount)))
            .OrderBy(t => t.Date)
            .ToList();

        var runningBalance = openingBalance;
        var entries = new List<StatementEntry>();
        foreach (var t in transactions)
        {
            runningBalance = Math.Round(runningBalance + t.Debit - t.Credit, 2);
            entries.Add(new StatementEntry
            {
                Date = t.Date,
                Document = t.Document,
                Debit = t.Debit,
                Credit = t.Credit,
                Balance = runningBalance
            });
        }

        return new CustomerStatement
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            FromDate = from,
            ToDate = to,
            OpeningBalance = openingBalance,
            Entries = entries,
            ClosingBalance = runningBalance
        };
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> Create(Customer customer)
    {
        customer.Id = 0;
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Customer customer)
    {
        var existing = await _db.Customers.FindAsync(id);
        if (existing is null) return NotFound();

        existing.Name = customer.Name;
        existing.ContactPerson = customer.ContactPerson;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.Address = customer.Address;
        existing.VatNumber = customer.VatNumber;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Office)},{nameof(UserRole.Manager)}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.Customers.FindAsync(id);
        if (existing is null) return NotFound();

        _db.Customers.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
