using JobCardApp.Api.Data;
using JobCardApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCardApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public PaymentsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Payment>>> GetAll([FromQuery] int? customerId)
    {
        var query = _db.Payments
            .Include(p => p.Customer)
            .Include(p => p.Allocations)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);

        return await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Payment>> Get(int id)
    {
        var payment = await _db.Payments
            .Include(p => p.Customer)
            .Include(p => p.Allocations).ThenInclude(a => a.Invoice)
            .FirstOrDefaultAsync(p => p.Id == id);

        return payment is null ? NotFound() : payment;
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Office)},{nameof(UserRole.Accounts)},{nameof(UserRole.Manager)}")]
    public async Task<ActionResult<Payment>> Create(Payment payment)
    {
        if (payment.Amount <= 0)
            return BadRequest("Payment amount must be greater than zero.");

        payment.Id = 0;
        payment.Customer = null;
        payment.CreatedAt = DateTime.UtcNow;
        payment.Allocations = new();

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = payment.Id }, payment);
    }

    /// <summary>
    /// Allocates part (or all) of an unallocated payment to an invoice.
    /// Server-enforced per §52: AllocatedAmount can't exceed what's left of
    /// the payment, or what's still outstanding on the invoice.
    /// </summary>
    [HttpPost("{id:int}/allocations")]
    [Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Office)},{nameof(UserRole.Accounts)},{nameof(UserRole.Manager)}")]
    public async Task<ActionResult<PaymentAllocation>> Allocate(int id, CreateAllocationRequest request)
    {
        var payment = await _db.Payments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payment is null) return NotFound();

        var invoice = await _db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Allocations)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId);
        if (invoice is null) return NotFound("Invoice not found.");

        if (invoice.CustomerId != payment.CustomerId)
            return BadRequest("This payment and invoice belong to different customers.");

        if (request.AllocatedAmount <= 0)
            return BadRequest("Allocated amount must be greater than zero.");

        if (request.AllocatedAmount > payment.UnallocatedAmount)
            return BadRequest($"Only {payment.UnallocatedAmount:C} of this payment is unallocated.");

        if (request.AllocatedAmount > invoice.OutstandingAmount)
            return BadRequest($"This invoice only has {invoice.OutstandingAmount:C} outstanding.");

        var allocation = new PaymentAllocation
        {
            PaymentId = payment.Id,
            InvoiceId = invoice.Id,
            AllocatedAmount = request.AllocatedAmount,
            AllocatedDate = DateTime.UtcNow
        };

        // Just Add() to the tracked set — do NOT also add to invoice.Allocations
        // by hand. EF's change tracker already performs relationship fixup
        // (allocation.InvoiceId matches the tracked `invoice`), so a manual
        // Add() here double-counts the same allocation in memory and throws
        // off the sum RecomputeInvoiceStatus relies on.
        _db.PaymentAllocations.Add(allocation);
        RecomputeInvoiceStatus(invoice);

        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = payment.Id }, allocation);
    }

    [HttpPost("allocations/{allocationId:int}/reverse")]
    [Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Office)},{nameof(UserRole.Accounts)},{nameof(UserRole.Manager)}")]
    public async Task<IActionResult> ReverseAllocation(int allocationId)
    {
        var allocation = await _db.PaymentAllocations.FindAsync(allocationId);
        if (allocation is null) return NotFound();

        var invoice = await _db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Allocations)
            .FirstOrDefaultAsync(i => i.Id == allocation.InvoiceId);
        if (invoice is null) return NotFound("Invoice not found.");

        invoice.Allocations.Remove(allocation);
        _db.PaymentAllocations.Remove(allocation);
        RecomputeInvoiceStatus(invoice);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// The only place Invoice.Status becomes Paid/PartiallyPaid — always
    /// derived from real allocations, never set directly by a client.
    /// </summary>
    private static void RecomputeInvoiceStatus(Invoice invoice)
    {
        if (invoice.Status == InvoiceStatus.Cancelled) return;

        if (invoice.OutstandingAmount <= 0.01m)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidOn ??= DateTime.UtcNow;
        }
        else if (invoice.AllocatedAmount > 0)
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
            invoice.PaidOn = null;
        }
        else
        {
            if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.PartiallyPaid)
                invoice.Status = InvoiceStatus.Sent;
            invoice.PaidOn = null;
        }
    }
}
