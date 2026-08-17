namespace JobCardApp.Shared.Models;

public enum UserRole
{
    Technician = 0,
    Office = 1,
    Accounts = 2,
    Manager = 3,
    Administrator = 4
}

public class User
{
    public int Id { get; set; }

    /// <summary>Login identifier — short and easy to type on a phone. Not an email address.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Optional contact email — not used for login.</summary>
    public string? Email { get; set; }

    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Technician;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
