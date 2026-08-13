using JobCardApp.Mobile.Pages;

namespace JobCardApp.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("jobcard-edit", typeof(JobCardEditPage));
    }
}
