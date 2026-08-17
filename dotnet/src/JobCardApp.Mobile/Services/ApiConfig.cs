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
    ///   Android emulator            -> http://10.0.2.2:5080
    ///   iOS Simulator (paired Mac)  -> http://{DevMachineLanIp}:5080 (Mac must be on the same network)
    ///   MacCatalyst/Windows (local) -> http://localhost:5080
    ///   Real device (any OS)        -> http://YOUR-LAN-IP:5080 (same network as this PC)
    /// </summary>
    public static string BaseUrl =>
#if ANDROID
        "http://10.0.2.2:5080";
#elif IOS
        $"http://{DevMachineLanIp}:5080";
#else
        "http://localhost:5080";
#endif
}
