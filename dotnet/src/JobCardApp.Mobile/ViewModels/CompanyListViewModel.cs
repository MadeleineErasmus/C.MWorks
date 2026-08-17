using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

public partial class CompanyListViewModel : ObservableObject
{
    private readonly ApiClient _api;

    public ObservableCollection<Company> Companies { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public CompanyListViewModel(ApiClient api) => _api = api;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var companies = await _api.GetCompaniesAsync() ?? new List<Company>();
            Companies.Clear();
            foreach (var c in companies) Companies.Add(c);
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
    private Task NewCompanyAsync() => Shell.Current.GoToAsync("company-edit");

    [RelayCommand]
    private Task OpenCompanyAsync(Company company)
        => Shell.Current.GoToAsync($"company-edit?id={company.Id}");
}
