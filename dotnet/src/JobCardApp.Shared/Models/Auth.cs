namespace JobCardApp.Shared.Models;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>If true, the issued token lives much longer and the client should persist the session across app restarts.</summary>
    public bool RememberMe { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public UserRole Role { get; set; }

    /// <summary>Echoes the request — tells the client whether to persist this session.</summary>
    public bool RememberMe { get; set; }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Technician;
}

/// <summary>User profile without the password hash — safe to return from the API.</summary>
public class UserProfile
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
