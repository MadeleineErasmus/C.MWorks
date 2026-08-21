using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

public partial class PaymentListViewModel : ObservableObject
{
    private readonly ApiClient _api;

    public ObservableCollection<Payment> Payments { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public PaymentListViewModel(ApiClient api) => _api = api;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var payments = await _api.GetPaymentsAsync() ?? new List<Payment>();
            Payments.Clear();
            foreach (var p in payments) Payments.Add(p);
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
    private Task NewPaymentAsync() => Shell.Current.GoToAsync("payment-edit");

    [RelayCommand]
    private Task OpenPaymentAsync(Payment payment)
        => Shell.Current.GoToAsync($"payment-edit?id={payment.Id}");
}
