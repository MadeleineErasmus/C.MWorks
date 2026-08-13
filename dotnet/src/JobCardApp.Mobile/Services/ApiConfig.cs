namespace JobCardApp.Mobile.Services;

public static class ApiConfig
{
    /// <summary>
    /// Where the API lives.
    ///   Android emulator -> http://10.0.2.2:5080
    ///   iOS simulator    -> http://localhost:5080
    ///   Real device      -> http://YOUR-LAN-IP:5080  (same Wi-Fi as your machine)
    /// </summary>
    public static string BaseUrl =>
#if ANDROID
        "http://10.0.2.2:5080";
#else
        "http://localhost:5080";
#endif
}
