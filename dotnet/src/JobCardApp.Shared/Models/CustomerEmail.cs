namespace JobCardApp.Shared.Models;

/// <summary>
/// An additional email address that should also receive quote/invoice PDFs
/// for this customer (e.g. an accounts department or a manager) — in
/// addition to, not instead of, the customer's primary <see cref="Customer.Email"/>.
/// </summary>
public class CustomerEmail
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
