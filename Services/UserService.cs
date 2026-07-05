using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.Auth;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Hash;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User> AddUserAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            UserName = request.UserName.Trim(),
            Name = request.Name.Trim(),
            Surname = request.Surname.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User?> GetUserByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users.FindAsync(new object?[] { id }, cancellationToken);
    }

    public async Task<User?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public async Task<bool> DeleteUserAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object?[] { id }, cancellationToken);

        if (user is null)
            return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
