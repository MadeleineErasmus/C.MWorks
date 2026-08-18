using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

[QueryProperty(nameof(JobCardId), "id")]
public partial class JobCardEditViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanComplete))]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    [NotifyPropertyChangedFor(nameof(CanInvoice))]
    [NotifyPropertyChangedFor(nameof(CanCreateQuote))]
    [NotifyPropertyChangedFor(nameof(HasJobCard))]
    private int jobCardId;
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string? description;
    [ObservableProperty] private string? siteAddress;
    [ObservableProperty] private string? technician;
    [ObservableProperty] private Customer? selectedCustomer;
    [ObservableProperty] private string customerSearchText = string.Empty;
    [ObservableProperty] private bool isCustomerSuggestionsVisible;
    [ObservableProperty] private Company? selectedCompany;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanComplete))]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    [NotifyPropertyChangedFor(nameof(CanInvoice))]
    [NotifyPropertyChangedFor(nameof(CanCreateQuote))]
    private JobCardStatus status = JobCardStatus.Open;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool includeCallOutFee;

    private decimal _defaultCallOutFee = 500m;
    // Suppresses the SiteAddress autofill while LoadAsync is restoring a
    // saved SelectedSite — that's a re-selection of existing state, not the
    // user picking a site, so it must not overwrite the job card's own saved
    // SiteAddress text (which may have drifted from the site's address).
    private bool _restoringSite;
    // Suppresses the search-text-driven filtering when CustomerSearchText is
    // being synced FROM a SelectedCustomer that was just set programmatically
    // (loading an existing job card, or a suggestion tap) — that's not the
    // user typing a search, so it must not reopen the suggestions list.
    private bool _syncingCustomerSearchText;
    // Same purpose as _syncingCustomerSearchText, for the Site autocomplete.
    private bool _syncingSiteSearchText;

    // New line entry fields
    [ObservableProperty] private string newLineDescription = string.Empty;
    [ObservableProperty] private string newLineQuantity = "1";
    [ObservableProperty] private string newLineUnitPrice = "0";
    [ObservableProperty] private LineKind newLineKind = LineKind.Labour;
    [ObservableProperty] private bool isPricingHistoryVisible;
    [ObservableProperty] private bool isPricingHistoryBusy;
    [ObservableProperty] private CustomerItem? selectedCustomerItem;
    [ObservableProperty] private string newItemName = string.Empty;
    [ObservableProperty] private CustomerSite? selectedSite;
    [ObservableProperty] private string siteSearchText = string.Empty;
    [ObservableProperty] private bool isSiteSuggestionsVisible;
    [ObservableProperty] private string newSiteName = string.Empty;

    public ObservableCollection<Customer> Customers { get; } = new();
    public ObservableCollection<Customer> FilteredCustomers { get; } = new();
    public ObservableCollection<Company> Companies { get; } = new();
    public ObservableCollection<JobCardLine> Lines { get; } = new();
    public ObservableCollection<PricingHistoryEntry> PricingHistory { get; } = new();
    public ObservableCollection<CustomerItem> CustomerItems { get; } = new();
    public ObservableCollection<CustomerSite> Sites { get; } = new();
    public ObservableCollection<CustomerSite> FilteredSites { get; } = new();
    public IReadOnlyList<LineKind> LineKinds { get; } = Enum.GetValues<LineKind>();

    public bool HasJobCard => JobCardId > 0;
    public bool CanComplete => JobCardId > 0 && Status is JobCardStatus.Open or JobCardStatus.InProgress;
    public bool CanCancel => JobCardId > 0 && Status is JobCardStatus.Open or JobCardStatus.InProgress or JobCardStatus.Completed;
    public bool CanInvoice => JobCardId > 0 && Status == JobCardStatus.Completed;
    public bool CanCreateQuote => JobCardId > 0 && Status != JobCardStatus.Cancelled;

    public decimal Subtotal => Lines.Sum(l => l.LineTotal);

    public JobCardEditViewModel(ApiClient api)
    {
        _api = api;
        Lines.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Subtotal));
    }

    partial void OnJobCardIdChanged(int value) => _ = LoadAsync();

    // Item picker is scoped to whichever customer is currently selected —
    // reload it whenever that changes, same as pricing history is scoped
    // per-customer.
    partial void OnSelectedCustomerChanged(Customer? value)
    {
        SelectedCustomerItem = null;
        SelectedSite = null;
        SiteSearchText = string.Empty;
        _ = LoadCustomerItemsAsync();
        _ = LoadSitesAsync();

        // Only sync the text box when a customer is actually picked (typing
        // a search, loading an existing job card). When value is null
        // because typing stopped matching (below), the text box already
        // holds exactly what was typed and must be left alone — syncing it
        // to "" here would erase it on every non-matching keystroke.
        if (value is not null)
        {
            _syncingCustomerSearchText = true;
            CustomerSearchText = value.Name;
            _syncingCustomerSearchText = false;
            IsCustomerSuggestionsVisible = false;
        }
    }

    /// <summary>
    /// Autocomplete: typing filters Customers to matches and shows the
    /// suggestion list; tapping a suggestion (SelectCustomerCommand) sets
    /// SelectedCustomer, which syncs this text back and hides the list again.
    /// </summary>
    partial void OnCustomerSearchTextChanged(string value)
    {
        if (_syncingCustomerSearchText) return;

        // Typing no longer matches the previously picked customer — that
        // selection is stale until they pick again.
        if (SelectedCustomer is not null && !string.Equals(SelectedCustomer.Name, value, StringComparison.OrdinalIgnoreCase))
            SelectedCustomer = null;

        FilteredCustomers.Clear();
        if (string.IsNullOrWhiteSpace(value))
        {
            IsCustomerSuggestionsVisible = false;
            return;
        }

        foreach (var c in Customers.Where(c => c.Name.Contains(value, StringComparison.OrdinalIgnoreCase)))
            FilteredCustomers.Add(c);
        IsCustomerSuggestionsVisible = FilteredCustomers.Count > 0;
    }

    [RelayCommand]
    private void SelectCustomer(Customer customer)
    {
        SelectedCustomer = customer;
    }

    /// <summary>
    /// Picking a saved site copies its address into the free-text
    /// SiteAddress field as a one-time autofill — SiteAddress stays a
    /// separate bound property, so the user can still freely edit it
    /// afterwards (e.g. for a one-off site or to tweak the picked address).
    /// </summary>
    partial void OnSelectedSiteChanged(CustomerSite? value)
    {
        if (value is not null && !_restoringSite)
            SiteAddress = value.Address;

        // Same reasoning as OnSelectedCustomerChanged: only sync the text box
        // forward when a site is actually picked/restored, never when it's
        // cleared as a side effect of typing (see OnSiteSearchTextChanged).
        if (value is not null)
        {
            _syncingSiteSearchText = true;
            SiteSearchText = value.Name;
            _syncingSiteSearchText = false;
            IsSiteSuggestionsVisible = false;
        }
    }

    /// <summary>
    /// Autocomplete for Site, mirroring the Customer field's behavior:
    /// typing filters Sites to matches and shows the suggestion list;
    /// tapping a suggestion (SelectSiteCommand) sets SelectedSite.
    /// </summary>
    partial void OnSiteSearchTextChanged(string value)
    {
        if (_syncingSiteSearchText) return;

        if (SelectedSite is not null && !string.Equals(SelectedSite.Name, value, StringComparison.OrdinalIgnoreCase))
            SelectedSite = null;

        FilteredSites.Clear();
        if (string.IsNullOrWhiteSpace(value))
        {
            IsSiteSuggestionsVisible = false;
            return;
        }

        foreach (var s in Sites.Where(s => s.Name.Contains(value, StringComparison.OrdinalIgnoreCase)))
            FilteredSites.Add(s);
        IsSiteSuggestionsVisible = FilteredSites.Count > 0;
    }

    [RelayCommand]
    private void SelectSite(CustomerSite site)
    {
        SelectedSite = site;
    }

    partial void OnIncludeCallOutFeeChanged(bool value)
    {
        var existing = Lines.FirstOrDefault(l => l.Kind == LineKind.CallOut);

        if (value && existing is null)
        {
            Lines.Add(new JobCardLine
            {
                Kind = LineKind.CallOut,
                Description = "Call-out fee",
                Quantity = 1,
                UnitPrice = _defaultCallOutFee
            });
        }
        else if (!value && existing is not null)
        {
            Lines.Remove(existing);
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var settings = await _api.GetBillingSettingsAsync();
            if (settings is not null) _defaultCallOutFee = settings.DefaultCallOutFee;

            var customers = await _api.GetCustomersAsync() ?? new List<Customer>();
            Customers.Clear();
            foreach (var c in customers) Customers.Add(c);

            var companies = await _api.GetCompaniesAsync() ?? new List<Company>();
            Companies.Clear();
            foreach (var c in companies) Companies.Add(c);

            if (JobCardId > 0)
            {
                var card = await _api.GetJobCardAsync(JobCardId);
                if (card is not null)
                {
                    Title = card.Title;
                    Description = card.Description;
                    Technician = card.Technician;
                    Status = card.Status;
                    SelectedCustomer = Customers.FirstOrDefault(c => c.Id == card.CustomerId);
                    SelectedCompany = Companies.FirstOrDefault(c => c.Id == card.CompanyId);

                    // OnSelectedCustomerChanged already kicked off a site
                    // reload above — await it directly here so Sites is
                    // populated before restoring which one was picked.
                    await LoadSitesAsync();
                    _restoringSite = true;
                    SelectedSite = Sites.FirstOrDefault(s => s.Id == card.SiteId);
                    _restoringSite = false;
                    // Set after SelectedSite so the restore above can't
                    // clobber it via the autofill guard race.
                    SiteAddress = card.SiteAddress;

                    Lines.Clear();
                    foreach (var line in card.Lines) Lines.Add(line);
                    IncludeCallOutFee = Lines.Any(l => l.Kind == LineKind.CallOut);
                }
            }
            else
            {
                // Left blank by default — the technician must actively pick
                // or search for the right customer rather than starting on
                // whichever one happened to load first.
                // Most businesses only have one company — default to it so
                // there's nothing extra to pick in the common case.
                SelectedCompany ??= Companies.FirstOrDefault();
                // Call-out fee is included by default on a new job card; the
                // technician can uncheck it to remove the line (§8).
                IncludeCallOutFee = true;
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

    private async Task LoadCustomerItemsAsync()
    {
        CustomerItems.Clear();
        if (SelectedCustomer is null) return;

        try
        {
            var items = await _api.GetCustomerItemsAsync(SelectedCustomer.Id) ?? new List<CustomerItem>();
            foreach (var item in items) CustomerItems.Add(item);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not load equipment", ex.Message, "OK");
        }
    }

    private async Task LoadSitesAsync()
    {
        Sites.Clear();
        if (SelectedCustomer is null) return;

        try
        {
            var sites = await _api.GetCustomerSitesAsync(SelectedCustomer.Id) ?? new List<CustomerSite>();
            foreach (var site in sites) Sites.Add(site);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not load sites", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private void AddLine()
    {
        if (string.IsNullOrWhiteSpace(NewLineDescription)) return;

        decimal.TryParse(NewLineQuantity, out var qty);
        decimal.TryParse(NewLineUnitPrice, out var price);

        Lines.Add(new JobCardLine
        {
            Kind = NewLineKind,
            Description = NewLineDescription.Trim(),
            Quantity = qty <= 0 ? 1 : qty,
            UnitPrice = price,
            CustomerItemId = SelectedCustomerItem?.Id,
            CustomerItem = SelectedCustomerItem
        });

        NewLineDescription = string.Empty;
        NewLineQuantity = "1";
        NewLineUnitPrice = "0";
        IsPricingHistoryVisible = false;
        // Each new line's item choice is independent — don't carry it forward.
        SelectedCustomerItem = null;
    }

    /// <summary>
    /// Lets the technician type a new equipment name inline while adding a
    /// line — created (or found, if it already exists for this customer)
    /// immediately server-side, then selected for the line being added.
    /// </summary>
    [RelayCommand]
    private async Task AddNewCustomerItemAsync()
    {
        if (SelectedCustomer is null || string.IsNullOrWhiteSpace(NewItemName)) return;

        try
        {
            var item = await _api.CreateCustomerItemAsync(SelectedCustomer.Id, NewItemName.Trim(), category: null);
            if (item is not null)
            {
                var existing = CustomerItems.FirstOrDefault(i => i.Id == item.Id);
                if (existing is null) CustomerItems.Add(item);
                SelectedCustomerItem = existing ?? item;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not add equipment", ex.Message, "OK");
            return;
        }

        NewItemName = string.Empty;
    }

    /// <summary>
    /// Lets the technician save the currently-typed SiteAddress as a new
    /// named site for this customer, right from the job card — the address
    /// text already in the field becomes the new site's address, so this is
    /// "not on the list? name it" rather than a separate address entry.
    /// </summary>
    [RelayCommand]
    private async Task AddSiteAsync()
    {
        if (SelectedCustomer is null || string.IsNullOrWhiteSpace(NewSiteName)) return;

        if (string.IsNullOrWhiteSpace(SiteAddress))
        {
            await Shell.Current.DisplayAlert("Missing address", "Enter the site address above before saving it as a new site.", "OK");
            return;
        }

        try
        {
            var site = await _api.AddCustomerSiteAsync(SelectedCustomer.Id, NewSiteName.Trim(), SiteAddress.Trim());
            if (site is not null)
            {
                var existing = Sites.FirstOrDefault(s => s.Id == site.Id);
                if (existing is null) Sites.Add(site);
                // Not suppressed: if this name already existed for the
                // customer, the server kept its original address — let the
                // normal autofill sync SiteAddress to that real saved value.
                SelectedSite = existing ?? site;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not add site", ex.Message, "OK");
            return;
        }

        NewSiteName = string.Empty;
    }

    [RelayCommand]
    private async Task CheckPricingHistoryAsync()
    {
        if (SelectedCustomer is null) return;

        IsPricingHistoryBusy = true;
        IsPricingHistoryVisible = true;
        try
        {
            var history = await _api.GetPricingHistoryAsync(SelectedCustomer.Id, NewLineDescription)
                ?? new List<PricingHistoryEntry>();
            PricingHistory.Clear();
            foreach (var entry in history) PricingHistory.Add(entry);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not load pricing history", ex.Message, "OK");
        }
        finally
        {
            IsPricingHistoryBusy = false;
        }
    }

    /// <summary>Fills in the previous price on the tap — never applied automatically (§7).</summary>
    [RelayCommand]
    private void UsePricingHistoryEntry(PricingHistoryEntry entry)
    {
        NewLineUnitPrice = entry.UnitPrice.ToString("0.##");
        IsPricingHistoryVisible = false;
    }

    [RelayCommand]
    private void RemoveLine(JobCardLine line)
    {
        Lines.Remove(line);

        // Keep the checkbox in sync if the call-out line was removed this way
        // instead of via the checkbox itself.
        if (line.Kind == LineKind.CallOut) IncludeCallOutFee = false;
    }

    [RelayCommand]
    private async Task CompleteAsync()
    {
        IsBusy = true;
        try
        {
            var updated = await _api.CompleteJobCardAsync(JobCardId);
            if (updated is not null) Status = updated.Status;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not complete", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelJobCardAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Cancel job card", "Are you sure you want to cancel this job card?", "Yes", "No");
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            var updated = await _api.CancelJobCardAsync(JobCardId);
            if (updated is not null) Status = updated.Status;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not cancel", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task InvoiceAsync()
    {
        IsBusy = true;
        try
        {
            var invoice = await _api.CreateInvoiceFromJobCardAsync(JobCardId);
            await Shell.Current.DisplayAlert("Invoice created",
                $"{invoice?.Number} — total {invoice?.Total:C}", "OK");
            var updated = await _api.GetJobCardAsync(JobCardId);
            if (updated is not null) Status = updated.Status;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not invoice", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateQuoteAsync()
    {
        IsBusy = true;
        try
        {
            var quote = await _api.CreateQuoteFromJobCardAsync(JobCardId);
            await Shell.Current.DisplayAlert("Quote created",
                $"{quote?.Number} — total {quote?.Total:C}", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not create quote", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Title) || SelectedCustomer is null)
        {
            await Shell.Current.DisplayAlert("Missing details", "Pick a customer and enter a title.", "OK");
            return;
        }

        var jobCard = new JobCard
        {
            Id = JobCardId,
            CustomerId = SelectedCustomer.Id,
            CompanyId = SelectedCompany?.Id,
            Title = Title.Trim(),
            Description = Description,
            SiteId = SelectedSite?.Id,
            SiteAddress = SiteAddress,
            Technician = Technician,
            Status = Status,
            Lines = Lines.ToList()
        };

        IsBusy = true;
        try
        {
            if (JobCardId > 0)
                await _api.UpdateJobCardAsync(jobCard);
            else
                await _api.CreateJobCardAsync(jobCard);

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
}
