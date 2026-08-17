using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

public partial class QuoteListViewModel : ObservableObject
{
    private readonly ApiClient _api;

    public ObservableCollection<Quote> Quotes { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public IReadOnlyList<QuoteStatusFilterOption> StatusFilterOptions { get; } =
        new QuoteStatusFilterOption[] { new("All statuses", null) }
            .Concat(Enum.GetValues<QuoteStatus>().Select(s => new QuoteStatusFilterOption(s.ToString(), s)))
            .ToList();

    [ObservableProperty] private QuoteStatusFilterOption selectedStatusFilter;

    public QuoteListViewModel(ApiClient api)
    {
        _api = api;
        selectedStatusFilter = StatusFilterOptions[0];
    }

    partial void OnSelectedStatusFilterChanged(QuoteStatusFilterOption value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var quotes = await _api.GetQuotesAsync(SelectedStatusFilter.Status) ?? new List<Quote>();
            Quotes.Clear();
            foreach (var q in quotes) Quotes.Add(q);
        }
        catch (Exception ex)
        {
            Error = $"Could not reach the server. {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenQuoteAsync(Quote quote) => Shell.Current.GoToAsync($"quote-edit?id={quote.Id}");

    [RelayCommand]
    private async Task AcceptAsync(Quote quote) => await ChangeStatusAsync(quote, QuoteStatus.Accepted);

    [RelayCommand]
    private async Task RejectAsync(Quote quote) => await ChangeStatusAsync(quote, QuoteStatus.Rejected);

    private async Task ChangeStatusAsync(Quote quote, QuoteStatus status)
    {
        try
        {
            await _api.SetQuoteStatusAsync(quote.Id, status);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Update failed", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task ConvertToInvoiceAsync(Quote quote)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Convert to invoice", $"Create an invoice from {quote.Number}?", "Yes", "No");
        if (!confirmed) return;

        try
        {
            var invoice = await _api.ConvertQuoteToInvoiceAsync(quote.Id);
            await Shell.Current.DisplayAlert("Invoice created",
                $"{invoice?.Number} — total {invoice?.Total:C}", "OK");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not convert", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Quote quote)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Delete quote", $"Delete draft quote {quote.Number}?", "Delete", "Cancel");
        if (!confirmed) return;

        try
        {
            await _api.DeleteQuoteAsync(quote.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Delete failed", ex.Message, "OK");
        }
    }
}

public record QuoteStatusFilterOption(string Label, QuoteStatus? Status);
