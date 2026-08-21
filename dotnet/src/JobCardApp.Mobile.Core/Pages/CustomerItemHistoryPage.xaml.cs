using JobCardApp.Mobile.ViewModels;

namespace JobCardApp.Mobile.Pages;

public partial class CustomerItemHistoryPage : ContentPage
{
    public CustomerItemHistoryPage(CustomerItemHistoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
