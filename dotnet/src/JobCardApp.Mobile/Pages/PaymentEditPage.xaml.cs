using JobCardApp.Mobile.ViewModels;

namespace JobCardApp.Mobile.Pages;

public partial class PaymentEditPage : ContentPage
{
    private readonly PaymentEditViewModel _vm;
    private bool _loaded;

    public PaymentEditPage(PaymentEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;

        // For a brand new payment there is no id query param, so load lookups here.
        if (_vm.PaymentId == 0) await _vm.LoadAsync();
    }
}
