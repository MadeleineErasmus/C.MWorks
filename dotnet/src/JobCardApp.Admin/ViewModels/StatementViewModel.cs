using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

[QueryProperty(nameof(CustomerId), "customerId")]
public partial class StatementViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty] private int customerId;
    [ObservableProperty] private string? customerName;
    [ObservableProperty] private DateTime fromDate = DateTime.UtcNow.AddMonths(-3);
    [ObservableProperty] private DateTime toDate = DateTime.UtcNow;
    [ObservableProperty] private decimal openingBalance;
    [ObservableProperty] private decimal closingBalance;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public ObservableCollection<StatementEntry> Entries { get; } = new();

    public StatementViewModel(ApiClient api) => _api = api;

    partial void OnCustomerIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (CustomerId <= 0) return;

        IsBusy = true;
        Error = null;
        try
        {
            var statement = await _api.GetCustomerStatementAsync(CustomerId, FromDate, ToDate);
            if (statement is not null)
            {
                CustomerName = statement.CustomerName;
                OpeningBalance = statement.OpeningBalance;
                ClosingBalance = statement.ClosingBalance;
                Entries.Clear();
                foreach (var e in statement.Entries) Entries.Add(e);
            }
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
}
