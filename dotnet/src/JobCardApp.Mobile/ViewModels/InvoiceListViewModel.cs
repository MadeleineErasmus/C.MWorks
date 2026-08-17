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

    public decimal OutstandingTotal => Invoices
        .Where(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.Overdue or InvoiceStatus.Draft)
        .Sum(i => i.Total);

    public InvoiceListViewModel(ApiClient api) => _api = api;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var invoices = await _api.GetInvoicesAsync() ?? new List<Invoice>();
            Invoices.Clear();
            foreach (var invoice in invoices) Invoices.Add(invoice);
            OnPropertyChanged(nameof(OutstandingTotal));
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
