using JobCardApp.Mobile.ViewModels;

namespace JobCardApp.Mobile.Pages;

public partial class InvoiceEditPage : ContentPage
{
    public InvoiceEditPage(InvoiceEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
