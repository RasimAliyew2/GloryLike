using GloryLikeBackend.Dtos.Auth;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);

            if (!result.Success)
                return Conflict(result);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPost("register/email/start")]
    public async Task<ActionResult<EmailRegistrationResponse>>
        StartEmailRegistration(
            [FromBody] StartEmailRegistrationRequest request,
            CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result =
            await _authService.StartEmailRegistrationAsync(
                request,
                cancellationToken);

        return ToEmailRegistrationActionResult(result);
    }

    [HttpGet("register/email/{verificationId:guid}/status")]
    public async Task<ActionResult<EmailRegistrationResponse>>
        GetEmailRegistrationStatus(
            Guid verificationId,
            CancellationToken cancellationToken)
    {
        var result =
            await _authService.GetEmailRegistrationStatusAsync(
                verificationId,
                cancellationToken);

        return ToEmailRegistrationActionResult(result);
    }

    [HttpPost("register/email/verify")]
    public async Task<ActionResult<EmailRegistrationResponse>>
        VerifyEmailRegistration(
            [FromBody] VerifyEmailRegistrationRequest request,
            CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result =
            await _authService.VerifyEmailRegistrationAsync(
                request,
                cancellationToken);

        return ToEmailRegistrationActionResult(result);
    }

    [HttpPost("register/email/resend")]
    public async Task<ActionResult<EmailRegistrationResponse>>
        ResendEmailRegistrationCode(
            [FromBody] ResendEmailRegistrationCodeRequest request,
            CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result =
            await _authService.ResendEmailRegistrationCodeAsync(
                request,
                cancellationToken);

        return ToEmailRegistrationActionResult(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(request, cancellationToken);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<AuthResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.ResetPasswordAsync(request, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    private ActionResult<EmailRegistrationResponse>
        ToEmailRegistrationActionResult(
            EmailRegistrationResponse result)
    {
        if (result.Success)
            return Ok(result);

        return result.ErrorCode switch
        {
            EmailRegistrationErrorCodes.DuplicateEmail
                or EmailRegistrationErrorCodes.Conflict =>
                    Conflict(result),

            EmailRegistrationErrorCodes.NotFound =>
                NotFound(result),

            EmailRegistrationErrorCodes.ResendTooEarly
                or EmailRegistrationErrorCodes.TooManyAttempts =>
                    StatusCode(
                        StatusCodes.Status429TooManyRequests,
                        result),

            EmailRegistrationErrorCodes.EmailDeliveryFailed =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    result),

            _ => BadRequest(result)
        };
    }
}
