using JobCardApp.Mobile.Pages;
using JobCardApp.Mobile.Services;

namespace JobCardApp.Mobile;

public partial class AppShell : Shell
{
    public AppShell(AuthState authState, ApiClient api)
    {
        InitializeComponent();
        Routing.RegisterRoute("jobcard-edit", typeof(JobCardEditPage));
        Routing.RegisterRoute("customer-edit", typeof(CustomerEditPage));
        Routing.RegisterRoute("company-edit", typeof(CompanyEditPage));
        Routing.RegisterRoute("payment-edit", typeof(PaymentEditPage));
        Routing.RegisterRoute("statement", typeof(StatementPage));

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
