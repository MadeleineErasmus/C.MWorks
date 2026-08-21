using JobCardApp.Mobile.Pages;
using JobCardApp.Mobile.Services;

namespace JobCardApp.Technician;

public partial class AppShell : Shell
{
    public AppShell(AuthState authState, ApiClient api)
    {
        InitializeComponent();
        // Only the routes reachable from this app — the quote/invoice/
        // payment/company/statement pages live in the Admin app.
        Routing.RegisterRoute("jobcard-edit", typeof(JobCardEditPage));
        Routing.RegisterRoute("customer-edit", typeof(CustomerEditPage));
        Routing.RegisterRoute("customer-item-history", typeof(CustomerItemHistoryPage));

        // Fires once the Shell is part of the visual tree, so Shell.Current
        // navigation is safe to use here.
        Loaded += async (_, _) => await RestoreSessionAsync(authState, api);
    }

    private static async Task RestoreSessionAsync(AuthState authState, ApiClient api)
    {
        var stored = await TokenStore.LoadAsync();
        if (stored is null || stored.ExpiresAt <= DateTime.UtcNow)
        {
            TokenStore.Clear();
            return;
        }

        authState.SetSession(stored);
        api.AttachToken(stored.Token);
        await Shell.Current.GoToAsync("//jobcards");
    }
}
