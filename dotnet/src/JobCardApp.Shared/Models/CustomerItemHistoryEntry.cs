namespace JobCardApp.Shared.Models;

/// <summary>
/// One historical job card line recorded against a customer's equipment item
/// (e.g. "replaced the battery" on a specific gate motor), newest first — lets
/// the customer/item detail view show every change made to that item over time.
/// </summary>
public class CustomerItemHistoryEntry
{
    public string JobCardReference { get; set; } = string.Empty;
    public string JobCardTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public LineKind Kind { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public DateTime Date { get; set; }
}
