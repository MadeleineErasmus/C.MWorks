using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

[QueryProperty(nameof(PaymentId), "id")]
public partial class PaymentEditViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNew))]
    [NotifyPropertyChangedFor(nameof(IsExisting))]
    private int paymentId;

    [ObservableProperty] private Customer? selectedCustomer;
    [ObservableProperty] private string amount = "0";
    [ObservableProperty] private string reference = string.Empty;
    [ObservableProperty] private string paymentMethod = string.Empty;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private bool isBusy;

    [ObservableProperty] private decimal unallocatedAmount;
    [ObservableProperty] private Invoice? selectedInvoiceToAllocate;
    [ObservableProperty] private string allocateAmount = "0";

    public bool IsNew => PaymentId <= 0;
    public bool IsExisting => PaymentId > 0;

    public ObservableCollection<Customer> Customers { get; } = new();
    public ObservableCollection<Invoice> OutstandingInvoices { get; } = new();
    public ObservableCollection<PaymentAllocation> Allocations { get; } = new();

    public PaymentEditViewModel(ApiClient api) => _api = api;

    partial void OnPaymentIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var customers = await _api.GetCustomersAsync() ?? new List<Customer>();
            Customers.Clear();
            foreach (var c in customers) Customers.Add(c);

            if (PaymentId > 0)
            {
                var payment = await _api.GetPaymentAsync(PaymentId);
                if (payment is not null)
                {
                    SelectedCustomer = Customers.FirstOrDefault(c => c.Id == payment.CustomerId);
                    Amount = payment.Amount.ToString("0.##");
                    Reference = payment.Reference ?? string.Empty;
                    PaymentMethod = payment.PaymentMethod ?? string.Empty;
                    Notes = payment.Notes;
                    UnallocatedAmount = payment.UnallocatedAmount;

                    Allocations.Clear();
                    foreach (var a in payment.Allocations) Allocations.Add(a);

                    await LoadOutstandingInvoicesAsync();
                }
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

    private async Task LoadOutstandingInvoicesAsync()
    {
        if (SelectedCustomer is null) return;

        var invoices = await _api.GetInvoicesAsync() ?? new List<Invoice>();
        OutstandingInvoices.Clear();
        foreach (var inv in invoices.Where(i =>
            i.CustomerId == SelectedCustomer.Id &&
            i.OutstandingAmount > 0 &&
            i.Status != InvoiceStatus.Cancelled))
        {
            OutstandingInvoices.Add(inv);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedCustomer is null || !decimal.TryParse(Amount, out var amt) || amt <= 0)
        {
            await Shell.Current.DisplayAlert("Missing details", "Pick a customer and enter a valid amount.", "OK");
            return;
        }

        var payment = new Payment
        {
            CustomerId = SelectedCustomer.Id,
            Amount = amt,
            Reference = Reference,
            PaymentMethod = PaymentMethod,
            Notes = Notes
        };

        IsBusy = true;
        try
        {
            await _api.CreatePaymentAsync(payment);
            await Shell.Current.GoToAsync("..");
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
    private async Task AllocateAsync()
    {
        if (SelectedInvoiceToAllocate is null || !decimal.TryParse(AllocateAmount, out var amt) || amt <= 0)
        {
            await Shell.Current.DisplayAlert("Missing details", "Pick an invoice and enter a valid amount.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            await _api.AllocatePaymentAsync(PaymentId, SelectedInvoiceToAllocate.Id, amt);
            AllocateAmount = "0";
            SelectedInvoiceToAllocate = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Allocation failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ReverseAsync(PaymentAllocation allocation)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Reverse allocation", $"Reverse this {allocation.AllocatedAmount:C} allocation?", "Yes", "No");
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await _api.ReverseAllocationAsync(allocation.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Reversal failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
