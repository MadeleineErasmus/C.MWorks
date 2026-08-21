using JobCardApp.Mobile.ViewModels;

namespace JobCardApp.Mobile.Pages;

public partial class QuoteEditPage : ContentPage
{
    public QuoteEditPage(QuoteEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
