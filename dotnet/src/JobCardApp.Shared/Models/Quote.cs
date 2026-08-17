namespace JobCardApp.Shared.Models;

public enum QuoteStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Rejected = 3,
    Expired = 4,
    ConvertedToInvoice = 5,
    Cancelled = 6
}

public class Quote
{
    public int Id { get; set; }

    /// <summary>Human friendly number, e.g. QUO-2026-0001.</summary>
    public string Number { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? JobCardId { get; set; }
    public JobCard? JobCard { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;

    public DateTime IssuedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresOn { get; set; }

    /// <summary>VAT rate as a fraction, e.g. 0.15 for 15%.</summary>
    public decimal TaxRate { get; set; }

    public string? Notes { get; set; }

    /// <summary>Set when POST /api/quotes/{id}/send succeeds — real email send, not a bare status flip.</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>The customer email address the quote was actually emailed to.</summary>
    public string? SentTo { get; set; }

    public List<QuoteLine> Lines { get; set; } = new();

    public decimal Subtotal => Math.Round(Lines.Sum(l => l.LineTotal), 2);
    public decimal TaxAmount => Math.Round(Subtotal * TaxRate, 2);
    public decimal Total => Subtotal + TaxAmount;

    public bool CanSend => Status == QuoteStatus.Draft;
    public bool CanAcceptOrReject => Status == QuoteStatus.Sent;
    public bool CanConvertToInvoice => Status is QuoteStatus.Draft or QuoteStatus.Sent or QuoteStatus.Accepted;
    public bool CanDelete => Status == QuoteStatus.Draft;

    /// <summary>Only a Sent quote can be reverted to Draft for editing — final states (Accepted/Rejected/ConvertedToInvoice/Expired/Cancelled) are not revisable.</summary>
    public bool CanRevise => Status == QuoteStatus.Sent;
}

public class QuoteLine
{
    public int Id { get; set; }
    public int QuoteId { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }

    public decimal LineTotal => Math.Round(Quantity * UnitPrice, 2);
}
