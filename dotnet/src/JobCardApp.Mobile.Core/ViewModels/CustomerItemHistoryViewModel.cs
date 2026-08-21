using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

[QueryProperty(nameof(ItemId), "id")]
public partial class CustomerItemHistoryViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty] private int itemId;
    [ObservableProperty] private string? itemName;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public ObservableCollection<CustomerItemHistoryEntry> Entries { get; } = new();

    public CustomerItemHistoryViewModel(ApiClient api) => _api = api;

    partial void OnItemIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (ItemId <= 0) return;

        IsBusy = true;
        Error = null;
        try
        {
            var item = await _api.GetCustomerItemAsync(ItemId);
            ItemName = item?.Name;

            var history = await _api.GetCustomerItemHistoryAsync(ItemId) ?? new List<CustomerItemHistoryEntry>();
            Entries.Clear();
            foreach (var e in history) Entries.Add(e);
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
