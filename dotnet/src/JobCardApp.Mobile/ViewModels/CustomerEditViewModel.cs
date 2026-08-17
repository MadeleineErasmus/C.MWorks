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
    [ObservableProperty] private string? selectedCategory;
    [ObservableProperty] private string newCategoryName = string.Empty;
    [ObservableProperty] private string newInvoiceEmail = string.Empty;
    [ObservableProperty] private string newSiteName = string.Empty;
    [ObservableProperty] private string newSiteAddress = string.Empty;

    public ObservableCollection<CustomerItem> Items { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<CustomerItemGroup> GroupedItems { get; } = new();
    public ObservableCollection<CustomerEmail> InvoiceEmails { get; } = new();
    public ObservableCollection<CustomerSite> Sites { get; } = new();

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
            RebuildCategories();
            RebuildGroupedItems();

            var emails = await _api.GetCustomerEmailsAsync(CustomerId) ?? new List<CustomerEmail>();
            InvoiceEmails.Clear();
            foreach (var email in emails) InvoiceEmails.Add(email);

            var sites = await _api.GetCustomerSitesAsync(CustomerId) ?? new List<CustomerSite>();
            Sites.Clear();
            foreach (var site in sites) Sites.Add(site);
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
    /// side so a repeated name (within the same category) never duplicates.
    /// Category comes from whichever of NewCategoryName (typed) or
    /// SelectedCategory (picked) was used most recently — they're kept
    /// mutually exclusive by the OnXChanged handlers below, so NewCategoryName
    /// wins whenever it's non-blank.
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

        var category = string.IsNullOrWhiteSpace(NewCategoryName) ? SelectedCategory : NewCategoryName.Trim();

        try
        {
            var item = await _api.CreateCustomerItemAsync(CustomerId, NewItemName.Trim(), category);
            if (item is not null && Items.All(i => i.Id != item.Id))
                Items.Add(item);
            NewItemName = string.Empty;
            NewCategoryName = string.Empty;
            RebuildCategories();
            RebuildGroupedItems();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not add equipment", ex.Message, "OK");
        }
    }

    /// <summary>Typing a brand-new category takes precedence — picking from the list afterwards clears it back out, and vice versa, so AddItemAsync always has one unambiguous value to send.</summary>
    partial void OnNewCategoryNameChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            SelectedCategory = null;
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            NewCategoryName = string.Empty;
    }

    private void RebuildCategories()
    {
        var categories = Items
            .Where(i => !string.IsNullOrWhiteSpace(i.Category))
            .Select(i => i.Category!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase);

        Categories.Clear();
        foreach (var category in categories) Categories.Add(category);
    }

    private void RebuildGroupedItems()
    {
        var groups = Items
            .GroupBy(i => string.IsNullOrWhiteSpace(i.Category) ? "Uncategorised" : i.Category!, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CustomerItemGroup(g.Key, g.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)));

        GroupedItems.Clear();
        foreach (var group in groups) GroupedItems.Add(group);
    }

    [RelayCommand]
    private Task ViewItemHistoryAsync(CustomerItem item)
        => Shell.Current.GoToAsync($"customer-item-history?id={item.Id}");

    /// <summary>
    /// Additional recipients for this customer's quote/invoice PDFs, on top
    /// of the primary Email field above — find-or-create server side so a
    /// repeated address never duplicates.
    /// </summary>
    [RelayCommand]
    private async Task AddInvoiceEmailAsync()
    {
        if (CustomerId <= 0)
        {
            await Shell.Current.DisplayAlert("Save the customer first", "Additional invoice emails can be added once the customer has been saved.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(NewInvoiceEmail)) return;

        try
        {
            var email = await _api.AddCustomerEmailAsync(CustomerId, NewInvoiceEmail.Trim());
            if (email is not null && InvoiceEmails.All(e => e.Id != email.Id))
                InvoiceEmails.Add(email);
            NewInvoiceEmail = string.Empty;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not add email", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task RemoveInvoiceEmailAsync(CustomerEmail email)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Remove email", $"Remove {email.Email} from this customer's invoice emails?", "Remove", "Cancel");
        if (!confirmed) return;

        try
        {
            await _api.DeleteCustomerEmailAsync(email.Id);
            var existing = InvoiceEmails.FirstOrDefault(e => e.Id == email.Id);
            if (existing is not null) InvoiceEmails.Remove(existing);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not remove email", ex.Message, "OK");
        }
    }

    /// <summary>
    /// This customer's saved sites/premises (e.g. "Head office", "Warehouse")
    /// — find-or-create server side by Name, so the job card screen can pick
    /// one to autofill its free-text site address without duplicating rows.
    /// </summary>
    [RelayCommand]
    private async Task AddSiteAsync()
    {
        if (CustomerId <= 0)
        {
            await Shell.Current.DisplayAlert("Save the customer first", "Sites can be added once the customer has been saved.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(NewSiteName) || string.IsNullOrWhiteSpace(NewSiteAddress)) return;

        try
        {
            var site = await _api.AddCustomerSiteAsync(CustomerId, NewSiteName.Trim(), NewSiteAddress.Trim());
            if (site is not null && Sites.All(s => s.Id != site.Id))
                Sites.Add(site);
            NewSiteName = string.Empty;
            NewSiteAddress = string.Empty;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not add site", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task RemoveSiteAsync(CustomerSite site)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Remove site", $"Remove {site.Name} from this customer's sites?", "Remove", "Cancel");
        if (!confirmed) return;

        try
        {
            await _api.DeleteCustomerSiteAsync(site.Id);
            var existing = Sites.FirstOrDefault(s => s.Id == site.Id);
            if (existing is not null) Sites.Remove(existing);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not remove site", ex.Message, "OK");
        }
    }

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

/// <summary>
/// One category's worth of equipment for the Equipment CollectionView's
/// IsGrouped display (e.g. all "Motor" items). Category is never null here —
/// items without one are bucketed under "Uncategorised" by RebuildGroupedItems.
/// </summary>
public class CustomerItemGroup : List<CustomerItem>
{
    public string Category { get; }

    public CustomerItemGroup(string category, IEnumerable<CustomerItem> items) : base(items)
        => Category = category;
}
