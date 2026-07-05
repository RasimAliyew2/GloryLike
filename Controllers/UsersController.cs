using GloryLikeBackend.Dtos.Auth;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> AddUser(
        [FromBody] RegisterRequest user,
        CancellationToken cancellationToken)
    {
        var result = await _userService.AddUserAsync(user, cancellationToken);
        result.PasswordHash = string.Empty;
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);

        if (user is null)
            return NotFound();

        user.PasswordHash = string.Empty;
        user.PasswordResetCodeHash = null;

        return Ok(user);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var deleted = await _userService.DeleteUserAsync(id, cancellationToken);

        if (!deleted)
            return NotFound();

        return Ok("User silindi");
    }
}
