using JobCardApp.Mobile.ViewModels;

namespace JobCardApp.Mobile.Pages;

public partial class JobCardEditPage : ContentPage
{
    private readonly JobCardEditViewModel _vm;
    private bool _loaded;

    public JobCardEditPage(JobCardEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;

        // For a brand new job card there is no id query param, so load lookups here.
        if (_vm.JobCardId == 0) await _vm.LoadAsync();
    }
}
