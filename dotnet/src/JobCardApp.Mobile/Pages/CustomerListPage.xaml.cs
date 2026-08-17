using JobCardApp.Mobile.ViewModels;

namespace JobCardApp.Mobile.Pages;

public partial class CustomerListPage : ContentPage
{
    private readonly CustomerListViewModel _vm;

    public CustomerListPage(CustomerListViewModel vm)
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
