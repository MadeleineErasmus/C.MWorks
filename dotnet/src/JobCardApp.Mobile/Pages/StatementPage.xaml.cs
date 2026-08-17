using JobCardApp.Mobile.ViewModels;

namespace JobCardApp.Mobile.Pages;

public partial class StatementPage : ContentPage
{
    public StatementPage(StatementViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
