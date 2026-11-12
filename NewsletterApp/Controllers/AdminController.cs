using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsletterApp.Data;

namespace NewsletterApp.Controllers;

// 관리자 화면. 일반 사용자는 수신 내역 화면으로 보낸다(쿠키 인증 AccessDeniedPath).
[Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
public sealed class AdminController(AppDbContext db) : Controller
{
    [HttpGet("/admin/users")]
    public IActionResult Users() => MenuView("users");

    [HttpGet("/admin/subscriptions")]
    public IActionResult AdminSubscriptions() => MenuView("admin_subscriptions");

    [HttpGet("/admin/gubuns")]
    public IActionResult Gubuns() => MenuView("gubuns");

    [HttpGet("/admin/gubuns/{id:long}")]
    public async Task<IActionResult> GubunDetail(long id)
    {
        if (!await db.Gubuns.AnyAsync(g => g.Id == id))
        {
            return Redirect("/admin/gubuns");
        }
        ViewData["GubunId"] = id;
        return MenuView("gubuns");
    }

    private IActionResult MenuView(string menu)
    {
        ViewData["Menu"] = menu;
        return View();
    }
}
