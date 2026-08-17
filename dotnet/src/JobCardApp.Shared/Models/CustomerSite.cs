namespace JobCardApp.Shared.Models;

/// <summary>
/// A physical site/premises belonging to a customer (e.g. "Head office",
/// "Warehouse", "Branch 2"), each with its own address. Purely a
/// convenience for picking a saved address onto a job card's free-text
/// SiteAddress field — there is no FK from JobCard to this table, and no
/// history tracking (unlike <see cref="CustomerItem"/>).
/// </summary>
public class CustomerSite
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
