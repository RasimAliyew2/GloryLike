using GloryLikeBackend.Data;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Hash.GloryLikeBackend.Helpers;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User> AddUserAsync(RegisterUserRequest request)
    {
        var user = new User
        {
            Name = request.Name,
            Surname = request.Surname,
            PasswordHash = PasswordHasher.HashPassword(request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var user = await GetUserByEmailAsync(email);

        if (user == null)
            return false;

        // Testing hash 
        return user.PasswordHash == PasswordHasher.HashPassword(password); 
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }
}