using System.Text.Json;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.Services;

/// <summary>Persists the logged-in session across app launches using platform secure storage.</summary>
public static class TokenStore
{
    private const string Key = "auth_session";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static Task SaveAsync(AuthResponse response)
        => SecureStorage.Default.SetAsync(Key, JsonSerializer.Serialize(response, JsonOptions));

    public static async Task<AuthResponse?> LoadAsync()
    {
        var json = await SecureStorage.Default.GetAsync(Key);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<AuthResponse>(json, JsonOptions);
        }
        catch (JsonException)
        {
            SecureStorage.Default.Remove(Key);
            return null;
        }
    }

    public static void Clear() => SecureStorage.Default.Remove(Key);
}
