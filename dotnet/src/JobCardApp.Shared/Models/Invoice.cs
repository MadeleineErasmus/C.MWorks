namespace JobCardApp.Shared.Models;

public enum InvoiceStatus
{
    Draft = 0,
    Sent = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4,

    /// <summary>Server-computed from payment allocations — not directly settable (see §0/§14).</summary>
    PartiallyPaid = 5
}

public class Invoice
{
    public int Id { get; set; }

    /// <summary>Human friendly number, e.g. INV-2026-0001.</summary>
    public string Number { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? JobCardId { get; set; }
    public JobCard? JobCard { get; set; }

    /// <summary>The issuing business entity — determines VAT and which banking/contact details print on the document.</summary>
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public DateTime IssuedOn { get; set; } = DateTime.UtcNow;
    public DateTime DueOn { get; set; } = DateTime.UtcNow.AddDays(30);
    public DateTime? PaidOn { get; set; }

    /// <summary>VAT rate as a fraction, e.g. 0.15 for 15%.</summary>
    public decimal TaxRate { get; set; } = 0.15m;

    public string? Notes { get; set; }

    public List<InvoiceLine> Lines { get; set; } = new();

    /// <summary>Populated via <see cref="PaymentAllocation"/> — see §13/§14.</summary>
    public List<PaymentAllocation> Allocations { get; set; } = new();

    public decimal Subtotal => Math.Round(Lines.Sum(l => l.LineTotal), 2);
    public decimal TaxAmount => Math.Round(Subtotal * TaxRate, 2);
    public decimal Total => Subtotal + TaxAmount;

    public decimal AllocatedAmount => Math.Round(Allocations.Sum(a => a.AllocatedAmount), 2);
    public decimal OutstandingAmount => Total - AllocatedAmount;
}

public class InvoiceLine
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }

    public decimal LineTotal => Math.Round(Quantity * UnitPrice, 2);
}
