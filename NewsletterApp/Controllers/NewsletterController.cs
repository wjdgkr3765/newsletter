using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NewsletterApp.Controllers;

// 첫 화면과 뉴스레터 수신 내역 화면
public sealed class NewsletterController : Controller
{
    [AllowAnonymous]
    [HttpGet("/")]
    public IActionResult Index() =>
        Redirect(User.Identity?.IsAuthenticated == true ? "/newsletters" : "/login");

    [HttpGet("/newsletters")]
    public IActionResult History()
    {
        ViewData["Menu"] = "history";
        return View();
    }
}
