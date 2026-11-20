using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsletterApp.Models.Requests;
using NewsletterApp.Services;

namespace NewsletterApp.Controllers.Api;

// docs/api.md 6.6 구독 관리 (관리자)
[Route("api/admin/subscriptions")]
[Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
public sealed class AdminSubscriptionApiController(SubscriptionService subscriptionService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPage(
        [FromQuery] long? gubunId, [FromQuery] int page = 1, [FromQuery] string? keyword = null) =>
        ToResult(await subscriptionService.GetForAdminAsync(gubunId, page, keyword));

    [HttpPost]
    public async Task<IActionResult> Set(AdminSubscriptionRequest request) =>
        ToResult(await subscriptionService.SetForAdminAsync(request));
}
