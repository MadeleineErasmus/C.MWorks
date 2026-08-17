namespace JobCardApp.Shared.Models;

/// <summary>
/// One historical invoice line for a customer, shown so office/technicians
/// can see (and choose to reuse) what was charged before — never applied
/// automatically. See §7.
/// </summary>
public class PricingHistoryEntry
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
}
