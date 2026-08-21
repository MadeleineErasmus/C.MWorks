namespace JobCardApp.Mobile.Services;

public static class ApiConfig
{
    /// <summary>
    /// This PC's LAN IP — needed because the iOS Simulator actually runs on
    /// a *paired Mac*, not this machine, so "localhost" from its point of
    /// view means the Mac's own loopback, not this PC. Update this if the
    /// PC's IP changes (DHCP) or you switch networks — check with
    /// `ipconfig` (look for the real Ethernet/Wi-Fi adapter, not a VPN
    /// virtual adapter).
    /// </summary>
    private const string DevMachineLanIp = "192.168.88.12";

    /// <summary>
    /// Where the API lives.
    ///   Android (emulator or real device) -> http://{DevMachineLanIp}:5080
    ///   iOS Simulator (paired Mac)         -> http://{DevMachineLanIp}:5080 (Mac must be on the same network)
    ///   MacCatalyst/Windows (local)        -> http://localhost:5080
    ///
    /// Note: 10.0.2.2 is a special alias that ONLY resolves on the Android
    /// emulator's virtual network — it means nothing to a real device, which
    /// is why a physical phone gets a connection timeout if this is set to
    /// it. The LAN IP below works from both the emulator and a real device,
    /// as long as the device's own network (Wi-Fi, not just USB/adb) is on
    /// the same LAN as this PC.
    /// </summary>
    public static string BaseUrl =>
#if ANDROID || IOS
        $"http://{DevMachineLanIp}:5080";
#else
        "http://localhost:5080";
#endif
}
