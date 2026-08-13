namespace JobCardApp.Shared.Models;

public enum InvoiceStatus
{
    Draft = 0,
    Sent = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4
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

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public DateTime IssuedOn { get; set; } = DateTime.UtcNow;
    public DateTime DueOn { get; set; } = DateTime.UtcNow.AddDays(30);
    public DateTime? PaidOn { get; set; }

    /// <summary>VAT rate as a fraction, e.g. 0.15 for 15%.</summary>
    public decimal TaxRate { get; set; } = 0.15m;

    public string? Notes { get; set; }

    public List<InvoiceLine> Lines { get; set; } = new();

    public decimal Subtotal => Math.Round(Lines.Sum(l => l.LineTotal), 2);
    public decimal TaxAmount => Math.Round(Subtotal * TaxRate, 2);
    public decimal Total => Subtotal + TaxAmount;
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
