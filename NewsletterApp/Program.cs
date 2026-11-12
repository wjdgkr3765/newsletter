using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsletterApp.Controllers.Api;
using NewsletterApp.Data;
using NewsletterApp.Models;
using NewsletterApp.Services;
using NewsletterApp.Services.Contents;

// .env(ConnectionStrings__Default, Gmail__Address, Gmail__AppPassword)를 환경 변수로 등록한다.
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(".env에 ConnectionStrings__Default가 없습니다.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddMemoryCache();
builder.Services.AddDataProtection().SetApplicationName("NewsletterApp");

builder.Services.AddSingleton<IClock, KstClock>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authentication.Cookies.ITicketStore, MemoryTicketStore>();
builder.Services.AddSingleton<IVerificationCodeService, VerificationCodeService>();
builder.Services.AddSingleton<ILoginAttemptService, LoginAttemptService>();
builder.Services.AddSingleton<IMailService, MailService>();
builder.Services.AddSingleton<IUnsubscribeTokenService, UnsubscribeTokenService>();
builder.Services.AddSingleton<ISendLockService, SendLockService>();
builder.Services.AddSingleton<DefaultContentBuilder>();
builder.Services.AddSingleton<ContentBuilderResolver>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MeService>();
builder.Services.AddScoped<AdminUserService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<GubunService>();
builder.Services.AddScoped<NewsletterQueryService>();
builder.Services.AddScoped<NewsletterSendService>();
builder.Services.AddHostedService<NewsletterSchedulerService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Newsletter.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/newsletters";
        options.Events.OnRedirectToLogin = context => RedirectOrJson(context.HttpContext, context.RedirectUri,
            StatusCodes.Status401Unauthorized, ErrorCodes.Unauthorized);
        options.Events.OnRedirectToAccessDenied = context => RedirectOrJson(context.HttpContext, "/newsletters",
            StatusCodes.Status403Forbidden, ErrorCodes.Forbidden);
    });
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<Microsoft.AspNetCore.Authentication.Cookies.ITicketStore>((options, store) => options.SessionStore = store);

// 기본은 로그인 필요. 비회원 화면·API에만 [AllowAnonymous]를 붙인다.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add(new AntiforgeryJsonResultFilter());
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // 오류 응답은 모두 api.md 공통 형식으로 직접 만든다. (ProblemDetails로 바꾸지 않음)
    options.SuppressMapClientErrors = true;
    options.InvalidModelStateResponseFactory = _ =>
        new BadRequestObjectResult(ApiControllerBase.CreateError(ErrorCodes.InvalidInput));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// 사용자에게 스택 트레이스를 보여주지 않는다. (security.md 9번)
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError(exception, "처리하지 못한 오류");
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    if (IsApi(context))
    {
        await context.Response.WriteAsJsonAsync(ApiControllerBase.CreateError(ErrorCodes.InternalError));
        return;
    }
    context.Response.ContentType = "text/plain; charset=utf-8";
    await context.Response.WriteAsync(ErrorCodes.GetMessage(ErrorCodes.InternalError));
}));

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    await next();
});

app.UseStatusCodePages(async statusContext =>
{
    var context = statusContext.HttpContext;
    if (IsApi(context) && context.Response.StatusCode == StatusCodes.Status404NotFound)
    {
        await context.Response.WriteAsJsonAsync(ApiControllerBase.CreateError(ErrorCodes.NotFound));
    }
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();
app.MapControllers();

app.Run();

static bool IsApi(HttpContext context) => context.Request.Path.StartsWithSegments("/api");

// API는 로그인 화면으로 보내지 않고 JSON으로 응답한다.
static Task RedirectOrJson(HttpContext context, string redirectUri, int status, string code)
{
    if (IsApi(context))
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(ApiControllerBase.CreateError(code));
    }
    context.Response.Redirect(redirectUri);
    return Task.CompletedTask;
}
