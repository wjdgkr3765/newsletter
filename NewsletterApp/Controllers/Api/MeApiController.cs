using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using NewsletterApp.Data.Entities;
using NewsletterApp.Models;
using NewsletterApp.Models.Requests;
using NewsletterApp.Models.Responses;
using NewsletterApp.Services;

namespace NewsletterApp.Controllers.Api;

// docs/api.md 6.2 내 정보. 대상은 항상 세션의 회원 id다.
[Route("api/me")]
public sealed class MeApiController(MeService meService, SubscriptionService subscriptionService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => ToResult(await meService.GetAsync(CurrentUserId));

    [HttpPost("email-codes")]
    public async Task<IActionResult> SendEmailChangeCode(EmailRequest request, CancellationToken cancellationToken) =>
        ToResult(await meService.SendEmailChangeCodeAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("email-codes/verify")]
    public IActionResult VerifyEmailChangeCode(EmailCodeVerifyRequest request) =>
        ToResult(meService.VerifyEmailChangeCode(CurrentUserId, request));

    [HttpPost("profile")]
    public async Task<IActionResult> UpdateProfile(ProfileRequest request)
    {
        ServiceResult<User> result = await meService.UpdateProfileAsync(CurrentUserId, request);
        if (!result.IsSuccess)
        {
            return Error(result.ErrorCode!);
        }
        User user = result.Data!;
        // 메뉴에 보이는 이름이 바뀌도록 세션을 새로 만든다. (로그인 유지)
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimsPrincipalExtensions.CreatePrincipal(user, CookieAuthenticationDefaults.AuthenticationScheme));
        return Ok(new ApiResponse<ProfileResponse>(new ProfileResponse(user.Name, user.Email)));
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(PasswordChangeRequest request) =>
        ToResult(await meService.ChangePasswordAsync(CurrentUserId, request));

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw(WithdrawRequest request)
    {
        ServiceResult<RedirectResponse> result = await meService.WithdrawAsync(CurrentUserId, request);
        if (result.IsSuccess)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        return ToResult(result);
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions() =>
        ToResult(await subscriptionService.GetMineAsync(CurrentUserId));

    [HttpPost("subscriptions")]
    public async Task<IActionResult> SetSubscription(MySubscriptionRequest request) =>
        ToResult(await subscriptionService.SetMineAsync(CurrentUserId, request));
}
