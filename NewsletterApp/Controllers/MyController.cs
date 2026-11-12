using Microsoft.AspNetCore.Mvc;

namespace NewsletterApp.Controllers;

// 내 구독, 내 정보 화면 (일반 사용자, 관리자)
public sealed class MyController : Controller
{
    [HttpGet("/my/subscriptions")]
    public IActionResult MySubscriptions()
    {
        ViewData["Menu"] = "my_subscriptions";
        return View();
    }

    [HttpGet("/my/profile")]
    public IActionResult Profile()
    {
        ViewData["Menu"] = "profile";
        return View();
    }
}
