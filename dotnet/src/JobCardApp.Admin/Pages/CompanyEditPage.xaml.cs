using JobCardApp.Mobile.ViewModels;

namespace JobCardApp.Mobile.Pages;

public partial class CompanyEditPage : ContentPage
{
    public CompanyEditPage(CompanyEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
