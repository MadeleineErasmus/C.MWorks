using CommunityToolkit.Mvvm.ComponentModel;
using JobCardApp.Mobile.Pages;
using JobCardApp.Mobile.Services;
using JobCardApp.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace JobCardApp.Technician;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<AuthState>();

        // Technicians manage customer/equipment info, not billing — the
        // shared Customer screen's "View statement" button stays hidden, and
        // sign-in lands on this app's own first tab.
        builder.Services.AddSingleton(new AppCapabilities
        {
            CanViewCustomerFinancials = false,
            HomeRoute = "//jobcards"
        });

        builder.Services.AddSingleton<JobCardListViewModel>();
        builder.Services.AddSingleton<CustomerListViewModel>();
        builder.Services.AddTransient<JobCardEditViewModel>();
        builder.Services.AddTransient<CustomerEditViewModel>();
        builder.Services.AddTransient<CustomerItemHistoryViewModel>();
        builder.Services.AddTransient<LoginViewModel>();

        builder.Services.AddSingleton<JobCardListPage>();
        builder.Services.AddSingleton<CustomerListPage>();
        builder.Services.AddTransient<JobCardEditPage>();
        builder.Services.AddTransient<CustomerEditPage>();
        builder.Services.AddTransient<CustomerItemHistoryPage>();
        builder.Services.AddTransient<LoginPage>();

        return builder.Build();
    }
}
