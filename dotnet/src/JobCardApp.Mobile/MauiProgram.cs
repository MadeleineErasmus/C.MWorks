using CommunityToolkit.Mvvm.ComponentModel;
using JobCardApp.Mobile.Pages;
using JobCardApp.Mobile.Services;
using JobCardApp.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace JobCardApp.Mobile;

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

        builder.Services.AddSingleton<JobCardListViewModel>();
        builder.Services.AddSingleton<InvoiceListViewModel>();
        builder.Services.AddSingleton<CustomerListViewModel>();
        builder.Services.AddSingleton<CompanyListViewModel>();
        builder.Services.AddSingleton<QuoteListViewModel>();
        builder.Services.AddSingleton<PaymentListViewModel>();
        builder.Services.AddTransient<JobCardEditViewModel>();
        builder.Services.AddTransient<CustomerEditViewModel>();
        builder.Services.AddTransient<CompanyEditViewModel>();
        builder.Services.AddTransient<PaymentEditViewModel>();
        builder.Services.AddTransient<QuoteEditViewModel>();
        builder.Services.AddTransient<InvoiceEditViewModel>();
        builder.Services.AddTransient<StatementViewModel>();
        builder.Services.AddTransient<CustomerItemHistoryViewModel>();
        builder.Services.AddTransient<LoginViewModel>();

        builder.Services.AddSingleton<JobCardListPage>();
        builder.Services.AddSingleton<InvoiceListPage>();
        builder.Services.AddSingleton<CustomerListPage>();
        builder.Services.AddSingleton<CompanyListPage>();
        builder.Services.AddSingleton<QuoteListPage>();
        builder.Services.AddSingleton<PaymentListPage>();
        builder.Services.AddTransient<JobCardEditPage>();
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
