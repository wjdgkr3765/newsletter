using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsletterApp.Data.Entities;
using NewsletterApp.Models;
using NewsletterApp.Models.Requests;
using NewsletterApp.Models.Responses;
using NewsletterApp.Services;

namespace NewsletterApp.Controllers.Api;

// docs/api.md 6.1 인증
[Route("api/auth")]
public sealed class AuthApiController(AuthService authService) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet("availability")]
    public async Task<IActionResult> Availability([FromQuery] string? loginId, [FromQuery] string? email) =>
        ToResult(await authService.CheckAvailabilityAsync(loginId, email));

    [AllowAnonymous]
    [HttpPost("email-codes")]
    public async Task<IActionResult> SendSignupCode(EmailRequest request, CancellationToken cancellationToken) =>
        ToResult(await authService.SendSignupCodeAsync(request, cancellationToken));

    [AllowAnonymous]
    [HttpPost("email-codes/verify")]
    public IActionResult VerifySignupCode(EmailCodeVerifyRequest request) =>
        ToResult(authService.VerifySignupCode(request));

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request) =>
        ToResult(await authService.RegisterAsync(request));

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        ServiceResult<User> result = await authService.LoginAsync(request);
        if (!result.IsSuccess)
        {
            return Error(result.ErrorCode!);
        }
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimsPrincipalExtensions.CreatePrincipal(result.Data!, CookieAuthenticationDefaults.AuthenticationScheme));
        return Ok(new ApiResponse<RedirectResponse>(new RedirectResponse("/newsletters")));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new ApiResponse<RedirectResponse>(new RedirectResponse("/login")));
    }

    [AllowAnonymous]
    [HttpPost("password-reset/codes")]
    public async Task<IActionResult> SendPasswordResetCode(PasswordResetCodeRequest request, CancellationToken cancellationToken) =>
        ToResult(await authService.SendPasswordResetCodeAsync(request, cancellationToken));

    [AllowAnonymous]
    [HttpPost("password-reset/codes/verify")]
    public async Task<IActionResult> VerifyPasswordResetCode(PasswordResetVerifyRequest request) =>
        ToResult(await authService.VerifyPasswordResetCodeAsync(request));

    [AllowAnonymous]
    [HttpPost("password-reset")]
    public async Task<IActionResult> ResetPassword(PasswordResetRequest request) =>
        ToResult(await authService.ResetPasswordAsync(request));
}
