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

    /// <summary>
    /// Tapping empty space on this page dismisses the keyboard. There's no
    /// "tap outside" notion in MAUI, so the recognizer sits on the root
    /// ScrollView and only ever fires for taps no interactive child claimed —
    /// buttons, entries and the suggestion rows' own recognizers still win.
    /// </summary>
    private void OnPageTapped(object? sender, TappedEventArgs e)
    {
        if (Content is not null) ClearFocus(Content);
    }

    /// <summary>
    /// Walks the visual tree for whatever currently holds focus and drops it.
    /// Each container type exposes its children differently — Border in
    /// particular is neither a Layout nor a ContentView, and every card on
    /// this page is one, so it has to be handled explicitly.
    /// </summary>
    private static void ClearFocus(IView view)
    {
        if (view is VisualElement { IsFocused: true } focused)
        {
            focused.Unfocus();
            return;
        }

        switch (view)
        {
            case Layout layout:
                foreach (var child in layout.Children)
                {
                    if (child is IView childView) ClearFocus(childView);
                }
                break;
            case ContentView { Content: IView contentChild }:
                ClearFocus(contentChild);
                break;
            case ScrollView { Content: IView scrollChild }:
                ClearFocus(scrollChild);
                break;
            case Border { Content: IView borderChild }:
                ClearFocus(borderChild);
                break;
        }
    }
}
