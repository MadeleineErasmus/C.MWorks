namespace JobCardApp.Shared.Models;

/// <summary>
/// A named, individually-trackable piece of equipment belonging to a customer
/// (e.g. "Front gate motor", "Back gate motor") so its own service history is
/// distinguishable from the customer's other equipment. Free-text name — there
/// is no fixed category/type taxonomy.
/// </summary>
public class CustomerItem
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
