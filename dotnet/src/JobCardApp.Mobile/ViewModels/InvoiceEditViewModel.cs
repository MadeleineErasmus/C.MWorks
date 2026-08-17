using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

/// <summary>
/// Detail/edit page for a single invoice — mirrors QuoteEditViewModel. Lines
/// are only editable while the invoice is Draft (CanEditLines); once Sent (or
/// Overdue) they're locked and the user must Revise back to Draft before
/// editing again. Paid/PartiallyPaid/Cancelled are final states and are
/// neither editable nor revisable. Send is the real email-with-PDF action,
/// not a bare status flip.
/// </summary>
[QueryProperty(nameof(InvoiceId), "id")]
public partial class InvoiceEditViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private int _customerId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditLines))]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyPropertyChangedFor(nameof(CanRevise))]
    [NotifyPropertyChangedFor(nameof(HasSentInfo))]
    private int invoiceId;

    [ObservableProperty] private string number = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditLines))]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyPropertyChangedFor(nameof(CanRevise))]
    private InvoiceStatus status = InvoiceStatus.Draft;
    [ObservableProperty] private string? customerName;
    [ObservableProperty] private string? companyName;
    [ObservableProperty] private DateTime issuedOn;
    [ObservableProperty] private DateTime dueOn = DateTime.UtcNow.AddDays(30);
    [ObservableProperty] private decimal taxRate;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private decimal outstandingAmount;
    [ObservableProperty] private DateTime? sentAt;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSentInfo))]
    private string? sentTo;
    [ObservableProperty] private bool isBusy;

    // New line entry fields
    [ObservableProperty] private string newLineDescription = string.Empty;
    [ObservableProperty] private string newLineQuantity = "1";
    [ObservableProperty] private string newLineUnitPrice = "0";

    public ObservableCollection<InvoiceLine> Lines { get; } = new();

    public bool CanEditLines => InvoiceId <= 0 || Status == InvoiceStatus.Draft;
    public bool CanSend => InvoiceId > 0 && Status == InvoiceStatus.Draft;
    public bool CanRevise => InvoiceId > 0 && Status is InvoiceStatus.Sent or InvoiceStatus.Overdue;
    public bool HasSentInfo => SentAt.HasValue && !string.IsNullOrWhiteSpace(SentTo);

    public decimal Subtotal => Lines.Sum(l => l.Quantity * l.UnitPrice);
    public decimal TaxAmount => Math.Round(Subtotal * TaxRate, 2);
    public decimal Total => Subtotal + TaxAmount;

    public InvoiceEditViewModel(ApiClient api)
    {
        _api = api;
        Lines.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(TaxAmount));
            OnPropertyChanged(nameof(Total));
        };
    }

    partial void OnInvoiceIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (InvoiceId <= 0) return;

        IsBusy = true;
        try
        {
            var invoice = await _api.GetInvoiceAsync(InvoiceId);
            if (invoice is null) return;

            Number = invoice.Number;
            Status = invoice.Status;
            CustomerName = invoice.Customer?.Name;
            CompanyName = invoice.Company?.Name;
            IssuedOn = invoice.IssuedOn;
            DueOn = invoice.DueOn;
            TaxRate = invoice.TaxRate;
            Notes = invoice.Notes;
            OutstandingAmount = invoice.OutstandingAmount;
            SentAt = invoice.SentAt;
            SentTo = invoice.SentTo;
            _customerId = invoice.CustomerId;

            Lines.Clear();
            foreach (var line in invoice.Lines)
            {
                // Attached before adding — InvoiceLine isn't an observable
                // model, so this has to be in place by the time the
                // CollectionView first renders the row, not after.
                await AttachLastInvoicedSummaryAsync(line);
                Lines.Add(line);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Load failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddLineAsync()
    {
        if (!CanEditLines || string.IsNullOrWhiteSpace(NewLineDescription)) return;

        decimal.TryParse(NewLineQuantity, out var qty);
        decimal.TryParse(NewLineUnitPrice, out var price);

        var line = new InvoiceLine
        {
            Description = NewLineDescription.Trim(),
            Quantity = qty <= 0 ? 1 : qty,
            UnitPrice = price
        };
        await AttachLastInvoicedSummaryAsync(line);
        Lines.Add(line);

        NewLineDescription = string.Empty;
        NewLineQuantity = "1";
        NewLineUnitPrice = "0";
    }

    /// <summary>
    /// Best-effort — this is a helpful hint, not critical path, so a failed
    /// lookup shouldn't block loading or adding a line.
    /// </summary>
    private async Task AttachLastInvoicedSummaryAsync(InvoiceLine line)
    {
        if (_customerId <= 0 || string.IsNullOrWhiteSpace(line.Description)) return;

        try
        {
            var history = await _api.GetPricingHistoryAsync(_customerId, line.Description);
            var match = history?.FirstOrDefault(h => h.InvoiceNumber != Number);
            if (match is not null)
            {
                line.LastInvoicedSummary =
                    $"Last invoiced: {match.Quantity:0.##} × {match.UnitPrice:C} ({match.InvoiceNumber}, {match.InvoiceDate:d})";
            }
        }
        catch
        {
            // Ignore — pricing history is a convenience, not required to edit an invoice.
        }
    }

    /// <summary>
    /// InvoiceLine isn't an observable model, so an in-place edit to an
    /// existing line's Quantity/UnitPrice doesn't refresh its LineTotal or
    /// the Subtotal/Tax/Total footer on its own — re-adding every line forces
    /// the CollectionView to re-render each row, and Lines.CollectionChanged
    /// already refreshes the footer totals.
    /// </summary>
    public void RecalculateTotals()
    {
        var lines = Lines.ToList();
        Lines.Clear();
        foreach (var l in lines) Lines.Add(l);
    }

    [RelayCommand]
    private void RemoveLine(InvoiceLine line)
    {
        if (!CanEditLines) return;
        Lines.Remove(line);
    }

    [RelayCommand]
    private async Task SaveChangesAsync()
    {
        if (!CanEditLines) return;

        var invoice = new Invoice
        {
            Id = InvoiceId,
            DueOn = DueOn,
            Notes = Notes,
            Lines = Lines.ToList()
        };

        IsBusy = true;
        try
        {
            var updated = await _api.UpdateInvoiceLinesAsync(invoice);
            if (updated is not null)
            {
                Lines.Clear();
                foreach (var line in updated.Lines) Lines.Add(line);
            }
            await Shell.Current.DisplayAlert("Saved", "Invoice changes saved.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Save failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Send invoice", $"Email {Number} to {CustomerName}?", "Send", "Cancel");
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            var updated = await _api.SendInvoiceAsync(InvoiceId);
            if (updated is not null)
            {
                Status = updated.Status;
                SentAt = updated.SentAt;
                SentTo = updated.SentTo;
            }
            await Shell.Current.DisplayAlert("Sent", $"Invoice emailed to {SentTo}.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not send", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ReviseAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Revise invoice", "This reverts the invoice to Draft so it can be edited, then sent again. Continue?", "Yes", "No");
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            var updated = await _api.ReviseInvoiceAsync(InvoiceId);
            if (updated is not null) Status = updated.Status;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not revise", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
