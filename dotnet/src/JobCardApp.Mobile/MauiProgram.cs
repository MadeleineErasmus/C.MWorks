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

        builder.Services.AddSingleton<JobCardListViewModel>();
        builder.Services.AddSingleton<InvoiceListViewModel>();
        builder.Services.AddTransient<JobCardEditViewModel>();

        builder.Services.AddSingleton<JobCardListPage>();
        builder.Services.AddSingleton<InvoiceListPage>();
        builder.Services.AddTransient<JobCardEditPage>();

        return builder.Build();
    }
}
