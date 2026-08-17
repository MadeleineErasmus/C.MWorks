namespace JobCardApp.Shared.Models;

/// <summary>Server-configured billing defaults, read from the API's own config — never hard-coded client-side.</summary>
public class BillingSettings
{
    public decimal TaxRate { get; set; }
    public int PaymentTermDays { get; set; }
    public decimal DefaultCallOutFee { get; set; }
}
