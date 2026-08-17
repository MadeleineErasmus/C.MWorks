using JobCardApp.Mobile.ViewModels;

namespace JobCardApp.Mobile.Pages;

public partial class CompanyListPage : ContentPage
{
    private readonly CompanyListViewModel _vm;

    public CompanyListPage(CompanyListViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
