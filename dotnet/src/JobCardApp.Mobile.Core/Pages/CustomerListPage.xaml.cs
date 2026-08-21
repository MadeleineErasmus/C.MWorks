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

    // iOS: the SearchBar's native search controller can hang onto keyboard
    // focus across a Shell push, leaving the field on the page navigated to
    // unable to receive typed input even though it looks focused. Releasing
    // it here on every way off this page (row tap, + New customer, back)
    // avoids that.
    protected override void OnDisappearing()
    {
        CustomerSearchBar.Unfocus();
        base.OnDisappearing();
    }
}
