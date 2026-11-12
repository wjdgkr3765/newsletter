using System.Security.Claims;
using NewsletterApp.Data.Entities;

namespace NewsletterApp.Controllers;

// 로그인 세션 클레임: 회원 id, 아이디, 이름, 관리자 여부(Role)
public static class ClaimsPrincipalExtensions
{
    public const string AdminRole = "Admin";
    public const string DisplayNameClaim = "display_name";

    public static long GetUserId(this ClaimsPrincipal principal) =>
        long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static string GetDisplayName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(DisplayNameClaim) ?? "";

    public static ClaimsPrincipal CreatePrincipal(User user, string scheme)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.LoginId),
            new(DisplayNameClaim, user.Name)
        };
        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, AdminRole));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme));
    }
}
