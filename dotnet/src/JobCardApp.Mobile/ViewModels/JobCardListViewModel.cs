using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

public partial class JobCardListViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly AuthState _authState;

    public ObservableCollection<JobCard> JobCards { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public string? SignedInAs => _authState.DisplayName is null
        ? null
        : $"{_authState.DisplayName} ({_authState.Role})";

    public JobCardListViewModel(ApiClient api, AuthState authState)
    {
        _api = api;
        _authState = authState;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _authState.Clear();
        _api.AttachToken(null);
        TokenStore.Clear();
        await Shell.Current.GoToAsync("//login");
    }

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
}
