using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsletterApp.Models.Requests;
using NewsletterApp.Services;

namespace NewsletterApp.Controllers.Api;

// docs/api.md 6.5 회원 관리 (관리자)
[Route("api/admin/users")]
[Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
public sealed class AdminUserApiController(AdminUserService adminUserService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPage([FromQuery] int page = 1, [FromQuery] string? keyword = null) =>
        ToResult(await adminUserService.GetPageAsync(CurrentUserId, page, keyword));

    [HttpPost("{id:long}/suspension")]
    public async Task<IActionResult> SetSuspension(long id, SuspensionRequest request) =>
        ToResult(await adminUserService.SetSuspensionAsync(CurrentUserId, id, request));
}
