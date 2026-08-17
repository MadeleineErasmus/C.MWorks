using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

[QueryProperty(nameof(CustomerId), "id")]
public partial class CustomerEditViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    private int customerId;

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string? contactPerson;
    [ObservableProperty] private string? email;
    [ObservableProperty] private string? phone;
    [ObservableProperty] private string? address;
    [ObservableProperty] private string? vatNumber;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string newItemName = string.Empty;

    public ObservableCollection<CustomerItem> Items { get; } = new();

    public bool CanDelete => CustomerId > 0;

    public CustomerEditViewModel(ApiClient api) => _api = api;

    partial void OnCustomerIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (CustomerId <= 0) return;

        IsBusy = true;
        try
        {
            var customer = await _api.GetCustomerAsync(CustomerId);
            if (customer is not null)
            {
                Name = customer.Name;
                ContactPerson = customer.ContactPerson;
                Email = customer.Email;
                Phone = customer.Phone;
                Address = customer.Address;
                VatNumber = customer.VatNumber;
            }

            var items = await _api.GetCustomerItemsAsync(CustomerId) ?? new List<CustomerItem>();
            Items.Clear();
            foreach (var item in items) Items.Add(item);
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

    /// <summary>
    /// Lets office/technicians add equipment directly on the customer, not
    /// only inline while creating a job card line — find-or-create server
    /// side so a repeated name never duplicates.
    /// </summary>
    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (CustomerId <= 0)
        {
            await Shell.Current.DisplayAlert("Save the customer first", "Equipment can be added once the customer has been saved.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(NewItemName)) return;

        try
        {
            var item = await _api.CreateCustomerItemAsync(CustomerId, NewItemName.Trim());
            if (item is not null && Items.All(i => i.Id != item.Id))
                Items.Add(item);
            NewItemName = string.Empty;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not add equipment", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private Task ViewItemHistoryAsync(CustomerItem item)
        => Shell.Current.GoToAsync($"customer-item-history?id={item.Id}");

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlert("Missing details", "Enter a customer name.", "OK");
            return;
        }

        var customer = new Customer
        {
            Id = CustomerId,
            Name = Name.Trim(),
            ContactPerson = ContactPerson,
            Email = Email,
            Phone = Phone,
            Address = Address,
            VatNumber = VatNumber
        };

        IsBusy = true;
        try
        {
            if (CustomerId > 0)
                await _api.UpdateCustomerAsync(customer);
            else
                await _api.CreateCustomerAsync(customer);

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
    private Task ViewStatementAsync() => Shell.Current.GoToAsync($"statement?customerId={CustomerId}");

    [RelayCommand]
    private async Task DeleteAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Delete customer", $"Delete {Name}? This cannot be undone.", "Delete", "Cancel");
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await _api.DeleteCustomerAsync(CustomerId);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Delete failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
