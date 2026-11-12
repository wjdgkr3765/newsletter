using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsletterApp.Models.Requests;
using NewsletterApp.Services;

namespace NewsletterApp.Controllers.Api;

// docs/api.md 6.4 수신 거부 (로그인 불필요)
[Route("api/unsubscribe")]
[AllowAnonymous]
public sealed class UnsubscribeApiController(SubscriptionService subscriptionService) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Unsubscribe(UnsubscribeRequest request) =>
        ToResult(await subscriptionService.UnsubscribeAsync(request));
}
