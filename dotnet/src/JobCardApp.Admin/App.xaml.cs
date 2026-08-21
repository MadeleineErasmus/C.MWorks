using JobCardApp.Mobile.Services;

namespace JobCardApp.Admin;

public partial class App : Application
{
    private readonly AuthState _authState;
    private readonly ApiClient _api;

    public App(AuthState authState, ApiClient api)
    {
        InitializeComponent();
        _authState = authState;
        _api = api;
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell(_authState, _api));
}
