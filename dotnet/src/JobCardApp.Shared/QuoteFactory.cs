using JobCardApp.Shared.Models;

namespace JobCardApp.Shared;

/// <summary>
/// Turns a job card into a draft quote, and a quote into a draft invoice.
/// Mirrors <see cref="InvoiceFactory"/> — kept as a separate type per §12:
/// quote/invoice business rules stay separate even though the shape is similar.
/// </summary>
public static class QuoteFactory
{
    public static Quote FromJobCard(JobCard jobCard, string number, decimal taxRate = 0.15m, int validDays = 30)
    {
        var now = DateTime.UtcNow;

        // Same rule as InvoiceFactory: the job card's company is authoritative for VAT.
        var resolvedTaxRate = jobCard.Company?.TaxRate ?? taxRate;

        return new Quote
        {
            Number = number,
            CustomerId = jobCard.CustomerId,
            JobCardId = jobCard.Id,
            CompanyId = jobCard.CompanyId,
            Status = QuoteStatus.Draft,
            IssuedOn = now,
            ExpiresOn = now.AddDays(validDays),
            TaxRate = resolvedTaxRate,
            Notes = $"For jobcard {jobCard.Reference}: {jobCard.Title}",
            Lines = jobCard.Lines.Select(l => new QuoteLine
            {
                Description = $"[{l.Kind}] {l.Description}",
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };
    }

    public static Invoice ToInvoice(Quote quote, string invoiceNumber, int paymentTermDays = 30)
    {
        var now = DateTime.UtcNow;

        return new Invoice
        {
            Number = invoiceNumber,
            CustomerId = quote.CustomerId,
            JobCardId = quote.JobCardId,
            CompanyId = quote.CompanyId,
            Status = InvoiceStatus.Draft,
            IssuedOn = now,
            DueOn = now.AddDays(paymentTermDays),
            TaxRate = quote.TaxRate,
            Notes = $"From quote {quote.Number}",
            Lines = quote.Lines.Select(l => new InvoiceLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };
    }
}
