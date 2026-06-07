using GloryLikeBackend.Models;

namespace GloryLikeBackend.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> AddUserAsync(RegisterUserRequest user);
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> DeleteUserAsync(int id);
    }
}
