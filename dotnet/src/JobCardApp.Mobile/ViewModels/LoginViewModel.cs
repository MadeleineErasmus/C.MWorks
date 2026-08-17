using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;

namespace JobCardApp.Mobile.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly AuthState _authState;

    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private bool rememberMe = true;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public LoginViewModel(ApiClient api, AuthState authState)
    {
        _api = api;
        _authState = authState;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Error = "Enter your username and password.";
            return;
        }

        IsBusy = true;
        Error = null;

        try
        {
            var response = await _api.LoginAsync(Username.Trim(), Password, RememberMe);
            if (response is null)
            {
                Error = "Invalid username or password.";
                return;
            }

            _authState.SetSession(response);
            _api.AttachToken(response.Token);

            // Only persist the session across app restarts if the user
            // asked to be remembered — otherwise this is in-memory only for
            // the current run, and TokenStore is explicitly cleared so a
            // previous "remembered" session can't linger.
            if (RememberMe)
                await TokenStore.SaveAsync(response);
            else
                TokenStore.Clear();

            Password = string.Empty;
            await Shell.Current.GoToAsync("//jobcards");
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
