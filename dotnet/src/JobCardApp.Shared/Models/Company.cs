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

    // Banking details, shown on invoices/statements for this company.
    public string? BankName { get; set; }
    public string? AccountHolder { get; set; }
    public string? AccountNumber { get; set; }
    public string? BranchCode { get; set; }
    public string? AccountType { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
