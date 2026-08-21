using CommunityToolkit.Mvvm.ComponentModel;
using JobCardApp.Mobile.Pages;
using JobCardApp.Mobile.Services;
using JobCardApp.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace JobCardApp.Admin;

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

        // Office staff handle billing, so the shared Customer screen's
        // "View statement" button is shown here, and sign-in lands on this
        // app's own first tab.
        builder.Services.AddSingleton(new AppCapabilities
        {
            CanViewCustomerFinancials = true,
            HomeRoute = "//quotes"
        });

        builder.Services.AddSingleton<InvoiceListViewModel>();
        builder.Services.AddSingleton<CustomerListViewModel>();
        builder.Services.AddSingleton<CompanyListViewModel>();
        builder.Services.AddSingleton<QuoteListViewModel>();
        builder.Services.AddSingleton<PaymentListViewModel>();
        builder.Services.AddTransient<CustomerEditViewModel>();
        builder.Services.AddTransient<CompanyEditViewModel>();
        builder.Services.AddTransient<PaymentEditViewModel>();
        builder.Services.AddTransient<QuoteEditViewModel>();
        builder.Services.AddTransient<InvoiceEditViewModel>();
        builder.Services.AddTransient<StatementViewModel>();
        builder.Services.AddTransient<CustomerItemHistoryViewModel>();
        builder.Services.AddTransient<LoginViewModel>();

        builder.Services.AddSingleton<InvoiceListPage>();
        builder.Services.AddSingleton<CustomerListPage>();
        builder.Services.AddSingleton<CompanyListPage>();
        builder.Services.AddSingleton<QuoteListPage>();
        builder.Services.AddSingleton<PaymentListPage>();
        builder.Services.AddTransient<CustomerEditPage>();
        builder.Services.AddTransient<CompanyEditPage>();
        builder.Services.AddTransient<PaymentEditPage>();
        builder.Services.AddTransient<QuoteEditPage>();
        builder.Services.AddTransient<InvoiceEditPage>();
        builder.Services.AddTransient<StatementPage>();
        builder.Services.AddTransient<CustomerItemHistoryPage>();
        builder.Services.AddTransient<LoginPage>();

        return builder.Build();
    }
}
