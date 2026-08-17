using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.Services;

/// <summary>Holds the current session in memory for the lifetime of the app run.</summary>
public class AuthState
{
    public string? Token { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public int? UserId { get; private set; }
    public string? Username { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Email { get; private set; }
    public UserRole? Role { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token) && ExpiresAt > DateTime.UtcNow;

    public void SetSession(AuthResponse response)
    {
        Token = response.Token;
        ExpiresAt = response.ExpiresAt;
        UserId = response.UserId;
        Username = response.Username;
        DisplayName = response.DisplayName;
        Email = response.Email;
        Role = response.Role;
    }

    public void Clear()
    {
        Token = null;
        ExpiresAt = null;
        UserId = null;
        Username = null;
        DisplayName = null;
        Email = null;
        Role = null;
    }
}
