using JobCardApp.Api.Data;
using JobCardApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCardApp.Api.Controllers;

/// <summary>
/// Administrator-only user management. This is how the <10-user team gets
/// provisioned — there is no public self-registration by design.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UsersController(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserProfile>>> GetAll()
        => await _db.Users
            .OrderBy(u => u.DisplayName)
            .Select(u => ToProfile(u))
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<UserProfile>> Create(CreateUserRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return Conflict("A user with that username already exists.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Role = request.Role
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), ToProfile(user));
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static UserProfile ToProfile(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        DisplayName = u.DisplayName,
        Role = u.Role,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt
    };
}
