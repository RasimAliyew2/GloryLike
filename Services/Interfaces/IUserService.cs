using GloryLikeBackend.Dtos.Auth;
using GloryLikeBackend.Models;

namespace GloryLikeBackend.Services.Interfaces;

public interface IUserService
{
    Task<User> AddUserAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken = default);
}
