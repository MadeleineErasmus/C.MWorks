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

    // New line entry fields
    [ObservableProperty] private string newLineDescription = string.Empty;
    [ObservableProperty] private string newLineQuantity = "1";
    [ObservableProperty] private string newLineUnitPrice = "0";
    [ObservableProperty] private LineKind newLineKind = LineKind.Labour;
    [ObservableProperty] private bool isPricingHistoryVisible;
    [ObservableProperty] private bool isPricingHistoryBusy;

    public ObservableCollection<Customer> Customers { get; } = new();
    public ObservableCollection<Company> Companies { get; } = new();
    public ObservableCollection<JobCardLine> Lines { get; } = new();
    public ObservableCollection<PricingHistoryEntry> PricingHistory { get; } = new();
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
                    SiteAddress = card.SiteAddress;
                    Technician = card.Technician;
                    Status = card.Status;
                    SelectedCustomer = Customers.FirstOrDefault(c => c.Id == card.CustomerId);
                    SelectedCompany = Companies.FirstOrDefault(c => c.Id == card.CompanyId);

                    Lines.Clear();
                    foreach (var line in card.Lines) Lines.Add(line);
                    IncludeCallOutFee = Lines.Any(l => l.Kind == LineKind.CallOut);
                }
            }
            else
            {
                SelectedCustomer ??= Customers.FirstOrDefault();
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
            UnitPrice = price
        });

        NewLineDescription = string.Empty;
        NewLineQuantity = "1";
        NewLineUnitPrice = "0";
        IsPricingHistoryVisible = false;
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
