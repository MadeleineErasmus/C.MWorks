using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

/// <summary>
/// Detail/edit page for a single quote — mirrors JobCardEditViewModel's
/// query-property/load-on-change shape. Lines are only editable while the
/// quote is Draft (CanEditLines); once Sent they're locked and the user must
/// Revise (Sent -> Draft) before editing again. Send is the real
/// email-with-PDF action, not a bare status flip.
/// </summary>
[QueryProperty(nameof(QuoteId), "id")]
public partial class QuoteEditViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditLines))]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyPropertyChangedFor(nameof(CanRevise))]
    [NotifyPropertyChangedFor(nameof(HasSentInfo))]
    private int quoteId;

    [ObservableProperty] private string number = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditLines))]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyPropertyChangedFor(nameof(CanRevise))]
    private QuoteStatus status = QuoteStatus.Draft;
    [ObservableProperty] private string? customerName;
    [ObservableProperty] private string? companyName;
    [ObservableProperty] private DateTime issuedOn;
    [ObservableProperty] private DateTime expiresOn = DateTime.UtcNow.AddDays(30);
    [ObservableProperty] private decimal taxRate;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private DateTime? sentAt;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSentInfo))]
    private string? sentTo;
    [ObservableProperty] private bool isBusy;

    // New line entry fields
    [ObservableProperty] private string newLineDescription = string.Empty;
    [ObservableProperty] private string newLineQuantity = "1";
    [ObservableProperty] private string newLineUnitPrice = "0";

    public ObservableCollection<QuoteLine> Lines { get; } = new();

    public bool CanEditLines => QuoteId <= 0 || Status == QuoteStatus.Draft;
    public bool CanSend => QuoteId > 0 && Status == QuoteStatus.Draft;
    public bool CanRevise => QuoteId > 0 && Status == QuoteStatus.Sent;
    public bool HasSentInfo => SentAt.HasValue && !string.IsNullOrWhiteSpace(SentTo);

    public decimal Subtotal => Lines.Sum(l => l.Quantity * l.UnitPrice);
    public decimal TaxAmount => Math.Round(Subtotal * TaxRate, 2);
    public decimal Total => Subtotal + TaxAmount;

    public QuoteEditViewModel(ApiClient api)
    {
        _api = api;
        Lines.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(TaxAmount));
            OnPropertyChanged(nameof(Total));
        };
    }

    partial void OnQuoteIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (QuoteId <= 0) return;

        IsBusy = true;
        try
        {
            var quote = await _api.GetQuoteAsync(QuoteId);
            if (quote is null) return;

            Number = quote.Number;
            Status = quote.Status;
            CustomerName = quote.Customer?.Name;
            CompanyName = quote.Company?.Name;
            IssuedOn = quote.IssuedOn;
            ExpiresOn = quote.ExpiresOn ?? DateTime.UtcNow.AddDays(30);
            TaxRate = quote.TaxRate;
            Notes = quote.Notes;
            SentAt = quote.SentAt;
            SentTo = quote.SentTo;

            Lines.Clear();
            foreach (var line in quote.Lines) Lines.Add(line);
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
    private void AddLine()
    {
        if (!CanEditLines || string.IsNullOrWhiteSpace(NewLineDescription)) return;

        decimal.TryParse(NewLineQuantity, out var qty);
        decimal.TryParse(NewLineUnitPrice, out var price);

        Lines.Add(new QuoteLine
        {
            Description = NewLineDescription.Trim(),
            Quantity = qty <= 0 ? 1 : qty,
            UnitPrice = price
        });

        NewLineDescription = string.Empty;
        NewLineQuantity = "1";
        NewLineUnitPrice = "0";
    }

    [RelayCommand]
    private void RemoveLine(QuoteLine line)
    {
        if (!CanEditLines) return;
        Lines.Remove(line);
    }

    [RelayCommand]
    private async Task SaveChangesAsync()
    {
        if (!CanEditLines) return;

        var quote = new Quote
        {
            Id = QuoteId,
            ExpiresOn = ExpiresOn,
            Notes = Notes,
            Lines = Lines.ToList()
        };

        IsBusy = true;
        try
        {
            var updated = await _api.UpdateQuoteLinesAsync(quote);
            if (updated is not null)
            {
                Lines.Clear();
                foreach (var line in updated.Lines) Lines.Add(line);
            }
            await Shell.Current.DisplayAlert("Saved", "Quote changes saved.", "OK");
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
            "Send quote", $"Email {Number} to {CustomerName}?", "Send", "Cancel");
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            var updated = await _api.SendQuoteAsync(QuoteId);
            if (updated is not null)
            {
                Status = updated.Status;
                SentAt = updated.SentAt;
                SentTo = updated.SentTo;
            }
            await Shell.Current.DisplayAlert("Sent", $"Quote emailed to {SentTo}.", "OK");
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
            "Revise quote", "This reverts the quote to Draft so it can be edited, then sent again. Continue?", "Yes", "No");
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            var updated = await _api.ReviseQuoteAsync(QuoteId);
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
