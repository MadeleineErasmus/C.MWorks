namespace JobCardApp.Shared.Models;

/// <summary>
/// A named, individually-trackable piece of equipment belonging to a customer
/// (e.g. "Front gate motor", "Back gate motor") so its own service history is
/// distinguishable from the customer's other equipment. Free-text name and
/// category — there is no fixed/shared taxonomy, and category is optional
/// (some items predate this field, so it must stay nullable).
/// </summary>
public class CustomerItem
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Free-text grouping typed by the user (e.g. "Motor"), independent per
    /// customer — not a shared/global taxonomy. Nullable: rows created before
    /// this field existed won't have one.
    /// </summary>
    public string? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Single-line label for pickers, e.g. "Motor: Front gate motor".</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Category) ? Name : $"{Category}: {Name}";
}
