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

    [ObservableProperty] private int jobCardId;
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string? description;
    [ObservableProperty] private string? siteAddress;
    [ObservableProperty] private string? technician;
    [ObservableProperty] private Customer? selectedCustomer;
    [ObservableProperty] private JobCardStatus status = JobCardStatus.Open;
    [ObservableProperty] private bool isBusy;

    // New line entry fields
    [ObservableProperty] private string newLineDescription = string.Empty;
    [ObservableProperty] private string newLineQuantity = "1";
    [ObservableProperty] private string newLineUnitPrice = "0";
    [ObservableProperty] private LineKind newLineKind = LineKind.Labour;

    public ObservableCollection<Customer> Customers { get; } = new();
    public ObservableCollection<JobCardLine> Lines { get; } = new();
    public IReadOnlyList<JobCardStatus> Statuses { get; } = Enum.GetValues<JobCardStatus>();
    public IReadOnlyList<LineKind> LineKinds { get; } = Enum.GetValues<LineKind>();

    public decimal Subtotal => Lines.Sum(l => l.LineTotal);

    public JobCardEditViewModel(ApiClient api)
    {
        _api = api;
        Lines.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Subtotal));
    }

    partial void OnJobCardIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var customers = await _api.GetCustomersAsync() ?? new List<Customer>();
            Customers.Clear();
            foreach (var c in customers) Customers.Add(c);

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

                    Lines.Clear();
                    foreach (var line in card.Lines) Lines.Add(line);
                }
            }
            else
            {
                SelectedCustomer ??= Customers.FirstOrDefault();
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
    }

    [RelayCommand]
    private void RemoveLine(JobCardLine line) => Lines.Remove(line);

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
