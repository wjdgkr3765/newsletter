using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NewsletterApp.Controllers;

// 비회원 화면: 로그인, 회원가입, 비밀번호 찾기. 로그인했으면 수신 내역으로 보낸다.
[AllowAnonymous]
public sealed class AccountController : Controller
{
    [HttpGet("/login")]
    public IActionResult Login() => GuestView();

    [HttpGet("/register")]
    public IActionResult Register() => GuestView();

    [HttpGet("/password-reset")]
    public IActionResult PasswordReset() => GuestView();

    private IActionResult GuestView() =>
        User.Identity?.IsAuthenticated == true ? Redirect("/newsletters") : View();
}
