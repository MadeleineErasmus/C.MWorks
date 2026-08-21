namespace JobCardApp.Shared.Models;

/// <summary>
/// One of the business entities that can issue job cards/invoices. Most
/// businesses will only ever have one, but some operate more than one legal
/// entity (e.g. a VAT-registered company alongside a non-VAT one) and need
/// to pick which is billing a given job.
/// </summary>
public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public bool IsVatRegistered { get; set; }
    public string? VatNumber { get; set; }

    /// <summary>VAT rate as a fraction (e.g. 0.15). Always 0 when <see cref="IsVatRegistered"/> is false — enforced server-side.</summary>
    public decimal TaxRate { get; set; }

    // Job pricing defaults, per company — different entities within the same
    // business can charge differently, so these live here rather than as a
    // single static app-wide setting. Both are prefills on the job card, never
    // a lock: the technician can always type a different price on the line.

    /// <summary>Default call-out fee prefilled on this company's job cards. 0 means "not configured" — the job card falls back to its own default.</summary>
    public decimal DefaultCallOutFee { get; set; }

    /// <summary>Default hourly/unit labour rate prefilled on this company's job card labour lines. 0 means "not configured".</summary>
    public decimal DefaultLabourRate { get; set; }

    // Banking details, shown on invoices/statements for this company.
    public string? BankName { get; set; }
    public string? AccountHolder { get; set; }
    public string? AccountNumber { get; set; }
    public string? BranchCode { get; set; }
    public string? AccountType { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
