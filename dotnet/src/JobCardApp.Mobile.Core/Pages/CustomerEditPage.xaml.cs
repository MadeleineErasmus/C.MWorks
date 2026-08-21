using JobCardApp.Mobile.ViewModels;

namespace JobCardApp.Mobile.Pages;

public partial class CustomerEditPage : ContentPage
{
    public CustomerEditPage(CustomerEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
