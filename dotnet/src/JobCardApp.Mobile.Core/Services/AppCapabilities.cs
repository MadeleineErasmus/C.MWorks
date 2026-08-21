namespace JobCardApp.Mobile.Services;

/// <summary>
/// Per-app feature flags for code that's shared between the Technician and
/// Admin apps but shouldn't behave identically in both — e.g. the shared
/// Customer screen shows financial history (View statement) only in the
/// Admin app, since technicians manage customer/equipment info, not billing.
/// Each app head registers its own instance in MauiProgram.
/// </summary>
public class AppCapabilities
{
    public bool CanViewCustomerFinancials { get; init; }

    /// <summary>
    /// Absolute Shell route of this app's default landing tab, used after a
    /// successful sign-in. The two apps have different tab sets — Technician
    /// starts on "//jobcards", Admin on "//quotes" — and the shared
    /// LoginViewModel can't hardcode either without breaking the other.
    /// </summary>
    public string HomeRoute { get; init; } = "//jobcards";
}
