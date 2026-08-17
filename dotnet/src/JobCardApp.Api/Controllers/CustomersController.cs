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

    /// <summary>
    /// This customer's individually-tracked equipment items (e.g. distinct
    /// gate motors) so a job card line or the customer page can pick one —
    /// each item's own history is what makes it worth tracking separately.
    /// </summary>
    [HttpGet("{id:int}/items")]
    public async Task<ActionResult<List<CustomerItem>>> GetItems(int id)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == id))
            return NotFound();

        return await _db.CustomerItems
            .Where(i => i.CustomerId == id)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Find-or-create: called both from the customer page's "add item" entry
    /// and inline while adding a job card line, so a name typed twice for the
    /// same customer must resolve to the same item rather than duplicating it.
    /// </summary>
    [HttpPost("{id:int}/items")]
    public async Task<ActionResult<CustomerItem>> CreateItem(int id, CustomerItem item)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == id))
            return NotFound();

        if (string.IsNullOrWhiteSpace(item.Name))
            return BadRequest("Name is required.");

        var name = item.Name.Trim();

        var existing = await _db.CustomerItems
            .FirstOrDefaultAsync(i => i.CustomerId == id && i.Name.ToLower() == name.ToLower());
        if (existing is not null)
            return existing;

        var created = new CustomerItem
        {
            CustomerId = id,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        _db.CustomerItems.Add(created);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetItems), new { id }, created);
    }

    /// <summary>
    /// Additional recipients for this customer's quote/invoice PDFs, on top
    /// of the primary <see cref="Customer.Email"/> — e.g. an accounts
    /// department or a manager who also needs a copy.
    /// </summary>
    [HttpGet("{id:int}/emails")]
    public async Task<ActionResult<List<CustomerEmail>>> GetEmails(int id)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == id))
            return NotFound();

        return await _db.CustomerEmails
            .Where(e => e.CustomerId == id)
            .OrderBy(e => e.Email)
            .ToListAsync();
    }

    /// <summary>
    /// Find-or-create, same reasoning as CreateItem: adding the same address
    /// twice for a customer must resolve to the same row, not duplicate it.
    /// Also rejects an address that's just a copy of the primary Email — that
    /// one's already covered and doesn't need a second row.
    /// </summary>
    [HttpPost("{id:int}/emails")]
    public async Task<ActionResult<CustomerEmail>> CreateEmail(int id, CustomerEmail email)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        if (string.IsNullOrWhiteSpace(email.Email))
            return BadRequest("Email is required.");

        var address = email.Email.Trim();
        if (!IsPlausibleEmail(address))
            return BadRequest("That doesn't look like a valid email address.");

        if (!string.IsNullOrWhiteSpace(customer.Email) &&
            string.Equals(customer.Email.Trim(), address, StringComparison.OrdinalIgnoreCase))
            return BadRequest("This is already the primary email for this customer.");

        var existing = await _db.CustomerEmails
            .FirstOrDefaultAsync(e => e.CustomerId == id && e.Email.ToLower() == address.ToLower());
        if (existing is not null)
            return existing;

        var created = new CustomerEmail
        {
            CustomerId = id,
            Email = address,
            CreatedAt = DateTime.UtcNow
        };
        _db.CustomerEmails.Add(created);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEmails), new { id }, created);
    }

    private static bool IsPlausibleEmail(string address)
    {
        // Deliberately loose — just enough to catch obvious typos ("foo",
        // "foo@") rather than fully validating RFC 5322.
        var at = address.IndexOf('@');
        return at > 0 && at < address.Length - 1 && !address.Contains(' ') && address.IndexOf('.', at) > at;
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
