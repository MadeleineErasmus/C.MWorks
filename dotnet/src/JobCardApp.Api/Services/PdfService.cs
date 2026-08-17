using JobCardApp.Shared.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JobCardApp.Api.Services;

/// <summary>
/// Renders a Quote or Invoice as a simple, printable PDF — used to attach to
/// the real email sent from POST /api/{quotes|invoices}/{id}/send (§ decision:
/// "real email... with a generated PDF of the quote/invoice attached").
/// Kept deliberately plain: header, bill-to, a line-item table, totals, notes.
/// </summary>
public class PdfService
{
    public byte[] RenderQuote(Quote quote) => Render(
        documentType: "Quote",
        number: quote.Number,
        company: quote.Company,
        customer: quote.Customer,
        issuedOn: quote.IssuedOn,
        secondaryDateLabel: "Expires",
        secondaryDate: quote.ExpiresOn,
        lines: quote.Lines.Select(l => (l.Description, l.Quantity, l.UnitPrice, l.LineTotal)),
        subtotal: quote.Subtotal,
        taxRate: quote.TaxRate,
        taxAmount: quote.TaxAmount,
        total: quote.Total,
        notes: quote.Notes);

    public byte[] RenderInvoice(Invoice invoice) => Render(
        documentType: "Invoice",
        number: invoice.Number,
        company: invoice.Company,
        customer: invoice.Customer,
        issuedOn: invoice.IssuedOn,
        secondaryDateLabel: "Due",
        secondaryDate: invoice.DueOn,
        lines: invoice.Lines.Select(l => (l.Description, l.Quantity, l.UnitPrice, l.LineTotal)),
        subtotal: invoice.Subtotal,
        taxRate: invoice.TaxRate,
        taxAmount: invoice.TaxAmount,
        total: invoice.Total,
        notes: invoice.Notes);

    private static byte[] Render(
        string documentType,
        string number,
        Company? company,
        Customer? customer,
        DateTime issuedOn,
        string secondaryDateLabel,
        DateTime? secondaryDate,
        IEnumerable<(string Description, decimal Quantity, decimal UnitPrice, decimal LineTotal)> lines,
        decimal subtotal,
        decimal taxRate,
        decimal taxAmount,
        decimal total,
        string? notes)
    {
        var lineList = lines.ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(company?.Name ?? string.Empty).FontSize(16).Bold();
                    if (!string.IsNullOrWhiteSpace(company?.Address)) col.Item().Text(company!.Address!);
                    if (!string.IsNullOrWhiteSpace(company?.Phone)) col.Item().Text(company!.Phone!);
                    if (!string.IsNullOrWhiteSpace(company?.Email)) col.Item().Text(company!.Email!);
                    if (company?.IsVatRegistered == true && !string.IsNullOrWhiteSpace(company.VatNumber))
                        col.Item().Text($"VAT No: {company.VatNumber}");

                    col.Item().PaddingTop(10).Text($"{documentType} {number}").FontSize(18).Bold();
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Bill to").SemiBold();
                            c.Item().Text(customer?.Name ?? string.Empty);
                            if (!string.IsNullOrWhiteSpace(customer?.Address)) c.Item().Text(customer!.Address!);
                            if (!string.IsNullOrWhiteSpace(customer?.Email)) c.Item().Text(customer!.Email!);
                        });

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"Issued: {issuedOn:d}");
                            if (secondaryDate.HasValue)
                                c.Item().Text($"{secondaryDateLabel}: {secondaryDate.Value:d}");
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Description").SemiBold();
                            header.Cell().AlignRight().Text("Qty").SemiBold();
                            header.Cell().AlignRight().Text("Unit price").SemiBold();
                            header.Cell().AlignRight().Text("Line total").SemiBold();

                            header.Cell().ColumnSpan(4).PaddingTop(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten1);
                        });

                        foreach (var line in lineList)
                        {
                            table.Cell().PaddingVertical(2).Text(line.Description);
                            table.Cell().PaddingVertical(2).AlignRight().Text(line.Quantity.ToString("0.##"));
                            table.Cell().PaddingVertical(2).AlignRight().Text(line.UnitPrice.ToString("C"));
                            table.Cell().PaddingVertical(2).AlignRight().Text(line.LineTotal.ToString("C"));
                        }
                    });

                    col.Item().AlignRight().Column(c =>
                    {
                        c.Item().Text($"Subtotal: {subtotal:C}");
                        c.Item().Text($"VAT ({taxRate:P0}): {taxAmount:C}");
                        c.Item().PaddingTop(4).Text($"Total: {total:C}").Bold().FontSize(13);
                    });

                    if (!string.IsNullOrWhiteSpace(notes))
                    {
                        col.Item().PaddingTop(10).Text("Notes").SemiBold();
                        col.Item().Text(notes);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
