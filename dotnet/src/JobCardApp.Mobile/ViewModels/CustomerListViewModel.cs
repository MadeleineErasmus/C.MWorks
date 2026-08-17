using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

public partial class CustomerListViewModel : ObservableObject
{
    private readonly ApiClient _api;

    public ObservableCollection<Customer> Customers { get; } = new();

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public CustomerListViewModel(ApiClient api) => _api = api;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var customers = await _api.GetCustomersAsync(SearchText) ?? new List<Customer>();
            Customers.Clear();
            foreach (var c in customers) Customers.Add(c);
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
    private Task NewCustomerAsync() => Shell.Current.GoToAsync("customer-edit");

    [RelayCommand]
    private Task OpenCustomerAsync(Customer customer)
        => Shell.Current.GoToAsync($"customer-edit?id={customer.Id}");
}
