using JobCardApp.Shared.Models;

namespace JobCardApp.Shared;

/// <summary>
/// Turns a completed jobcard into a draft invoice. Kept in Shared so the app
/// can preview the same numbers the API will produce.
/// </summary>
public static class InvoiceFactory
{
    public static Invoice FromJobCard(JobCard jobCard, string number, decimal taxRate = 0.15m, int paymentTermDays = 30)
    {
        var now = DateTime.UtcNow;

        return new Invoice
        {
            Number = number,
            CustomerId = jobCard.CustomerId,
            JobCardId = jobCard.Id,
            Status = InvoiceStatus.Draft,
            IssuedOn = now,
            DueOn = now.AddDays(paymentTermDays),
            TaxRate = taxRate,
            Notes = $"For jobcard {jobCard.Reference}: {jobCard.Title}",
            Lines = jobCard.Lines.Select(l => new InvoiceLine
            {
                Description = $"[{l.Kind}] {l.Description}",
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };
    }

    public static string NextReference(string prefix, int sequence, DateTime? when = null)
        => $"{prefix}-{(when ?? DateTime.UtcNow):yyyy}-{sequence:D4}";
}
