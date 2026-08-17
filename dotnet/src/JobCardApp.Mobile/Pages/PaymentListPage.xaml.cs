using JobCardApp.Mobile.ViewModels;

namespace JobCardApp.Mobile.Pages;

public partial class PaymentListPage : ContentPage
{
    private readonly PaymentListViewModel _vm;

    public PaymentListPage(PaymentListViewModel vm)
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
