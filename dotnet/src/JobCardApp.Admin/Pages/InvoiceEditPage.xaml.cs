using JobCardApp.Mobile.ViewModels;

namespace JobCardApp.Mobile.Pages;

public partial class InvoiceEditPage : ContentPage
{
    private readonly InvoiceEditViewModel _vm;

    public InvoiceEditPage(InvoiceEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    // InvoiceLine isn't an observable model, so editing an existing line's
    // Quantity/UnitPrice in place needs an explicit nudge to refresh
    // LineTotal and the Subtotal/Tax/Total footer — see
    // InvoiceEditViewModel.RecalculateTotals for why.
    private void OnLineFieldUnfocused(object sender, FocusEventArgs e) => _vm.RecalculateTotals();
}
