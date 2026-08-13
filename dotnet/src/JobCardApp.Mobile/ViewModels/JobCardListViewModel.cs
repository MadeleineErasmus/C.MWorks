using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

public partial class JobCardListViewModel : ObservableObject
{
    private readonly ApiClient _api;

    public ObservableCollection<JobCard> JobCards { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public JobCardListViewModel(ApiClient api) => _api = api;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var cards = await _api.GetJobCardsAsync() ?? new List<JobCard>();
            JobCards.Clear();
            foreach (var card in cards) JobCards.Add(card);
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
    private Task NewJobCardAsync() => Shell.Current.GoToAsync("jobcard-edit");

    [RelayCommand]
    private Task OpenJobCardAsync(JobCard jobCard)
        => Shell.Current.GoToAsync($"jobcard-edit?id={jobCard.Id}");

    [RelayCommand]
    private async Task InvoiceAsync(JobCard jobCard)
    {
        try
        {
            var invoice = await _api.CreateInvoiceFromJobCardAsync(jobCard.Id);
            await Shell.Current.DisplayAlert("Invoice created",
                $"{invoice?.Number} — total {invoice?.Total:C}", "OK");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Could not invoice", ex.Message, "OK");
        }
    }
}
