using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

public partial class InvoiceListViewModel : ObservableObject
{
    private readonly ApiClient _api;

    public ObservableCollection<Invoice> Invoices { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;
    [ObservableProperty] private decimal outstandingTotal;

    public IReadOnlyList<InvoiceStatusFilterOption> StatusFilterOptions { get; } =
        new InvoiceStatusFilterOption[] { new("All statuses", null) }
            .Concat(Enum.GetValues<InvoiceStatus>().Select(s => new InvoiceStatusFilterOption(s.ToString(), s)))
            .ToList();

    [ObservableProperty] private InvoiceStatusFilterOption selectedStatusFilter;

    public InvoiceListViewModel(ApiClient api)
    {
        _api = api;
        selectedStatusFilter = StatusFilterOptions[0];
    }

    partial void OnSelectedStatusFilterChanged(InvoiceStatusFilterOption value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var invoices = await _api.GetInvoicesAsync(SelectedStatusFilter.Status) ?? new List<Invoice>();
            Invoices.Clear();
            foreach (var invoice in invoices) Invoices.Add(invoice);

            // Outstanding is a running business total, not "sum of whatever the
            // status filter happens to be showing right now" — so it's worked
            // out from its own unfiltered fetch rather than from Invoices above.
            var allInvoices = SelectedStatusFilter.Status is null
                ? invoices
                : await _api.GetInvoicesAsync() ?? new List<Invoice>();
            OutstandingTotal = allInvoices
                .Where(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.Overdue or InvoiceStatus.Draft)
                .Sum(i => i.Total);
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
    private Task OpenInvoiceAsync(Invoice invoice) => Shell.Current.GoToAsync($"invoice-edit?id={invoice.Id}");
}

public record InvoiceStatusFilterOption(string Label, InvoiceStatus? Status);
