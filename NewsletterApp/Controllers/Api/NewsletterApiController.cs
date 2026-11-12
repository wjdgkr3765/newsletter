using Microsoft.AspNetCore.Mvc;
using NewsletterApp.Services;

namespace NewsletterApp.Controllers.Api;

// docs/api.md 6.3 뉴스레터 수신 내역
[Route("api/newsletters")]
public sealed class NewsletterApiController(NewsletterQueryService queryService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPage([FromQuery] int page = 1) =>
        ToResult(await queryService.GetPageAsync(CurrentUserId, User.IsInRole(ClaimsPrincipalExtensions.AdminRole), page));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id) =>
        ToResult(await queryService.GetAsync(CurrentUserId, User.IsInRole(ClaimsPrincipalExtensions.AdminRole), id));
}
