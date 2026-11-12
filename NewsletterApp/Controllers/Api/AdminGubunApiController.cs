using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsletterApp.Models.Requests;
using NewsletterApp.Services;

namespace NewsletterApp.Controllers.Api;

// docs/api.md 6.7 뉴스레터 설정, 6.8 오늘 뉴스레터 (관리자)
[Route("api/admin/gubuns")]
[Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
public sealed class AdminGubunApiController(GubunService gubunService, NewsletterSendService sendService)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => ToResult(await gubunService.GetAllAsync());

    [HttpPost]
    public async Task<IActionResult> Create(GubunCreateRequest request) =>
        ToResult(await gubunService.CreateAsync(request));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id) => ToResult(await gubunService.GetAsync(id));

    [HttpPost("{id:long}/config")]
    public async Task<IActionResult> SaveConfig(long id, GubunConfigRequest request) =>
        ToResult(await gubunService.SaveConfigAsync(id, request));

    [HttpGet("{id:long}/today")]
    public async Task<IActionResult> GetToday(long id) => ToResult(await sendService.GetTodayAsync(id));

    // 브라우저를 닫아도 발송이 끝까지 가도록 요청 취소 토큰을 쓰지 않는다.
    [HttpPost("{id:long}/today/send")]
    public async Task<IActionResult> Send(long id) =>
        ToResult(await sendService.CreateAndSendAsync(id, CancellationToken.None));

    [HttpPost("{id:long}/today/resend")]
    public async Task<IActionResult> Resend(long id) =>
        ToResult(await sendService.ResendAsync(id, CancellationToken.None));
}
