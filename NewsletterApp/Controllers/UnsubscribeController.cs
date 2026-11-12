using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsletterApp.Services;

namespace NewsletterApp.Controllers;

// 수신 거부 화면. 화면을 열기만 해서는 구독을 바꾸지 않는다.
[AllowAnonymous]
public sealed class UnsubscribeController(SubscriptionService subscriptionService) : Controller
{
    [HttpGet("/unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromQuery] string? token)
    {
        ViewData["GubunName"] = await subscriptionService.FindUnsubscribeGubunNameAsync(token);
        ViewData["Token"] = token ?? "";
        return View();
    }
}
