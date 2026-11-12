using Microsoft.EntityFrameworkCore;
using NewsletterApp.Data.Entities;
using NewsletterApp.Models;
using NewsletterApp.Models.Requests;

namespace NewsletterApp.Tests.Services;

// 발송, 다시 보내기, 수신 내역, 구독, 수신 거부, 종류 설정, 회원 관리
public sealed class NewsletterServiceTests
{
    [Fact]
    public async Task Send_NoSubscriber_DoesNotCreate()
    {
        using var context = new TestScope();
        Gubun gubun = context.AddGubun("유가 정보");
        var result = await context.SendService().CreateAndSendAsync(gubun.Id, CancellationToken.None);
        Assert.Equal(ErrorCodes.NoSubscriber, result.ErrorCode);
        Assert.Empty(context.CreateDb().Newsletters);
    }

    [Fact]
    public async Task Send_SkipsSuspendedAndRecordsOnlySuccess()
    {
        using var context = new TestScope();
        Gubun gubun = context.AddGubun("유가 정보");
        User ok = context.AddUser("okuser11");
        User failing = context.AddUser("failuser1");
        User suspended = context.AddUser("stopuser1", isSuspended: true);
        context.Subscribe(ok, gubun);
        context.Subscribe(failing, gubun);
        context.Subscribe(suspended, gubun);
        context.Mail.FailAddresses.Add(failing.Email);

        var result = await context.SendService().CreateAndSendAsync(gubun.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.ReceivedCount);
        Assert.Equal(1, result.Data.FailedCount);
        var mail = Assert.Single(context.Mail.Sent);
        Assert.Equal("[유가 정보] 2026-11-12", mail.Subject);
        Assert.Contains("수신 거부", mail.Html);
        Assert.Contains("https://localhost:7105/unsubscribe?token=", mail.Html);
        await using var db = context.CreateDb();
        Newsletter newsletter = await db.Newsletters.SingleAsync();
        Assert.DoesNotContain("수신 거부", newsletter.Content);
        Assert.Equal(new DateOnly(2026, 11, 12), newsletter.SendDay);
        Assert.Equal(ok.Id, (await db.Historys.SingleAsync()).UserId);
    }

    [Fact]
    public async Task Send_SecondTimeSameDay_Rejected()
    {
        using var context = new TestScope();
        Gubun gubun = context.AddGubun("유가 정보");
        context.Subscribe(context.AddUser("hong1234"), gubun);
        await context.SendService().CreateAndSendAsync(gubun.Id, CancellationToken.None);
        var second = await context.SendService().CreateAndSendAsync(gubun.Id, CancellationToken.None);
        Assert.Equal(ErrorCodes.NewsletterExists, second.ErrorCode);
    }

    [Fact]
    public async Task Send_KstDayBoundary_NewDayAllowed()
    {
        using var context = new TestScope();
        Gubun gubun = context.AddGubun("유가 정보");
        context.Subscribe(context.AddUser("hong1234"), gubun);
        context.Clock.UtcNow = new DateTime(2026, 11, 12, 14, 59, 0, DateTimeKind.Utc); // 한국 23:59
        await context.SendService().CreateAndSendAsync(gubun.Id, CancellationToken.None);
        context.Clock.UtcNow = new DateTime(2026, 11, 12, 15, 0, 0, DateTimeKind.Utc);  // 한국 다음날 00:00
        var next = await context.SendService().CreateAndSendAsync(gubun.Id, CancellationToken.None);
        Assert.True(next.IsSuccess);
        Assert.Equal(2, context.CreateDb().Newsletters.Count());
    }

    [Fact]
    public async Task Send_AllFail_KeepsNewsletterAndAllowsResend()
    {
        using var context = new TestScope();
        Gubun gubun = context.AddGubun("유가 정보");
        context.Subscribe(context.AddUser("hong1234"), gubun);
        context.Mail.IsConnectFailing = true;

        var result = await context.SendService().CreateAndSendAsync(gubun.Id, CancellationToken.None);

        Assert.Equal(ErrorCodes.SendFailed, result.ErrorCode);
        Assert.Single(context.CreateDb().Newsletters);
        var today = await context.SendService().GetTodayAsync(gubun.Id);
        Assert.True(today.Data!.Exists);
        Assert.Equal(0, today.Data.ReceivedCount);
        Assert.Equal(1, today.Data.PendingCount);

        context.Mail.IsConnectFailing = false;
        var resend = await context.SendService().ResendAsync(gubun.Id, CancellationToken.None);
        Assert.Equal(1, resend.Data!.ReceivedCount);
    }

    [Fact]
    public async Task Resend_SendsOnlyPendingIncludingNewSubscribers()
    {
        using var context = new TestScope();
        Gubun gubun = context.AddGubun("유가 정보");
        User first = context.AddUser("first111");
        context.Subscribe(first, gubun);
        await context.SendService().CreateAndSendAsync(gubun.Id, CancellationToken.None);
        context.Subscribe(context.AddUser("second11"), gubun);
        context.Mail.Sent.Clear();

        var resend = await context.SendService().ResendAsync(gubun.Id, CancellationToken.None);

        Assert.Equal(1, resend.Data!.ReceivedCount);
        Assert.Equal("second11@example.com", Assert.Single(context.Mail.Sent).To);
        var again = await context.SendService().ResendAsync(gubun.Id, CancellationToken.None);
        Assert.Equal(ErrorCodes.NoPendingSubscriber, again.ErrorCode);
    }

    [Fact]
    public async Task Resend_NoTodayNewsletter_NotFound()
    {
        using var context = new TestScope();
        Gubun gubun = context.AddGubun("유가 정보");
        var result = await context.SendService().ResendAsync(gubun.Id, CancellationToken.None);
        Assert.Equal(ErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Send_ContentFetchFails_DoesNotSend()
    {
        using var context = new TestScope();
        Gubun gubun = context.AddGubun("날씨 정보");
        context.Subscribe(context.AddUser("hong1234"), gubun);
        context.ContentBuilders.Add(new FailingContentBuilder("날씨 정보"));
        var result = await context.SendService().CreateAndSendAsync(gubun.Id, CancellationToken.None);
        Assert.Equal(ErrorCodes.ContentFetchFailed, result.ErrorCode);
        Assert.Empty(context.CreateDb().Newsletters);
        Assert.Empty(context.Mail.Sent);
    }

    [Fact]
    public async Task Send_Locked_SendingInProgress()
    {
        using var context = new TestScope();
        Gubun gubun = context.AddGubun("유가 정보");
        context.Subscribe(context.AddUser("hong1234"), gubun);
        using (context.SendLocks.TryAcquire(gubun.Id))
        {
            var result = await context.SendService().CreateAndSendAsync(gubun.Id, CancellationToken.None);
            Assert.Equal(ErrorCodes.SendingInProgress, result.ErrorCode);
        }
        Assert.True((await context.SendService().CreateAndSendAsync(gubun.Id, CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task Scheduled_OnlyInUseAndMatchingTime()
    {
        using var context = new TestScope();
        Gubun matching = context.AddGubun("유가 정보", new TimeOnly(16, 0), isUse: true);
        Gubun notUsed = context.AddGubun("날씨 정보", new TimeOnly(16, 0), isUse: false);
        Gubun otherTime = context.AddGubun("환율 정보", new TimeOnly(16, 1), isUse: true);
        User user = context.AddUser("hong1234");
        context.Subscribe(user, matching);
        context.Subscribe(user, notUsed);
        context.Subscribe(user, otherTime);

        await context.SendService().SendScheduledAsync(new TimeOnly(16, 0), CancellationToken.None);

        Assert.Equal(matching.Id, Assert.Single(context.CreateDb().Newsletters).GubunId);
    }

    [Fact]
    public async Task History_UserSeesOnlyOwnAdminSeesSent()
    {
        using var context = new TestScope();
        Gubun oil = context.AddGubun("유가 정보");
        Gubun weather = context.AddGubun("날씨 정보");
        User hong = context.AddUser("hong1234");
        User kim = context.AddUser("kim12345");
        User admin = context.AddUser("admin01", isAdmin: true);
        context.Subscribe(hong, oil);
        context.Subscribe(kim, weather);
        await context.SendService().CreateAndSendAsync(oil.Id, CancellationToken.None);
        await context.SendService().CreateAndSendAsync(weather.Id, CancellationToken.None);
        Gubun failed = context.AddGubun("환율 정보");
        context.Subscribe(kim, failed);
        context.Mail.IsConnectFailing = true;
        await context.SendService().CreateAndSendAsync(failed.Id, CancellationToken.None);

        var hongPage = await context.QueryService().GetPageAsync(hong.Id, false, 1);
        var adminPage = await context.QueryService().GetPageAsync(admin.Id, true, 1);

        var hongItem = Assert.Single(hongPage.Data!.Items);
        Assert.Equal("유가 정보", hongItem.GubunName);
        Assert.Null(hongItem.SubscriberCount);
        Assert.Equal(2, adminPage.Data!.TotalCount);
        Assert.All(adminPage.Data.Items, item => Assert.Equal(1, item.SubscriberCount));

        long weatherId = context.CreateDb().Newsletters.Single(n => n.GubunId == weather.Id).Id;
        Assert.Equal(ErrorCodes.NotFound, (await context.QueryService().GetAsync(hong.Id, false, weatherId)).ErrorCode);
        Assert.True((await context.QueryService().GetAsync(admin.Id, true, weatherId)).IsSuccess);
        Assert.Equal(ErrorCodes.InvalidInput, (await context.QueryService().GetPageAsync(hong.Id, false, 2)).ErrorCode);
        Assert.Equal(ErrorCodes.InvalidInput, (await context.QueryService().GetPageAsync(hong.Id, false, 0)).ErrorCode);
    }

    [Fact]
    public async Task Unsubscribe_ByToken()
    {
        using var context = new TestScope();
        Gubun gubun = context.AddGubun("유가 정보");
        User user = context.AddUser("hong1234");
        context.Subscribe(user, gubun);
        string token = context.Tokens.Create(user.Id, gubun.Id);
        var service = context.SubscriptionService();

        Assert.Equal("유가 정보", await service.FindUnsubscribeGubunNameAsync(token));
        Assert.Single(context.CreateDb().Subscriptions);
        Assert.True((await service.UnsubscribeAsync(new UnsubscribeRequest(token))).IsSuccess);
        Assert.Empty(context.CreateDb().Subscriptions);
        Assert.True((await service.UnsubscribeAsync(new UnsubscribeRequest(token))).IsSuccess);
        Assert.Equal(ErrorCodes.InvalidLink, (await service.UnsubscribeAsync(new UnsubscribeRequest("bad"))).ErrorCode);
        string goneUser = context.Tokens.Create(999, gubun.Id);
        Assert.Equal(ErrorCodes.InvalidLink, (await service.UnsubscribeAsync(new UnsubscribeRequest(goneUser))).ErrorCode);
    }

    [Fact]
    public async Task MySubscription_ToggleIsIdempotent()
    {
        using var context = new TestScope();
        Gubun gubun = context.AddGubun("유가 정보");
        User user = context.AddUser("hong1234");
        var service = context.SubscriptionService();
        Assert.True((await service.SetMineAsync(user.Id, new MySubscriptionRequest(gubun.Id, true))).IsSuccess);
        Assert.True((await service.SetMineAsync(user.Id, new MySubscriptionRequest(gubun.Id, true))).IsSuccess);
        Assert.Single(context.CreateDb().Subscriptions);
        var mine = await service.GetMineAsync(user.Id);
        var item = Assert.Single(mine.Data!.Items);
        Assert.True(item.IsSubscribed);
        Assert.Null(item.SendTime);
        Assert.Equal(ErrorCodes.NotFound, (await service.SetMineAsync(user.Id, new MySubscriptionRequest(999, true))).ErrorCode);
    }

    [Fact]
    public async Task AdminSubscription_ExcludesSuspended()
    {
        using var context = new TestScope();
        Gubun gubun = context.AddGubun("유가 정보");
        context.AddUser("hong1234");
        User suspended = context.AddUser("stopuser1", isSuspended: true);
        var service = context.SubscriptionService();
        var all = await service.GetForAdminAsync(null, 1);
        var byGubun = await service.GetForAdminAsync(gubun.Id, 1);
        Assert.Null(Assert.Single(all.Data!.Items).IsSubscribed);
        Assert.False(Assert.Single(byGubun.Data!.Items).IsSubscribed);
        var result = await service.SetForAdminAsync(new AdminSubscriptionRequest(suspended.Id, gubun.Id, true));
        Assert.Equal(ErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Gubun_CreateAndSaveConfig()
    {
        using var context = new TestScope();
        var service = context.GubunService();
        var created = await service.CreateAsync(new GubunCreateRequest(" 유가 정보 ", "16:00", false));
        Assert.True(created.IsSuccess);
        Assert.Equal(ErrorCodes.DuplicateGubunName,
            (await service.CreateAsync(new GubunCreateRequest("유가 정보", "16:00", false))).ErrorCode);
        Assert.Equal(ErrorCodes.InvalidInput,
            (await service.CreateAsync(new GubunCreateRequest("환율", "24:00", false))).ErrorCode);
        Gubun noConfig = context.AddGubun("날씨 정보");
        var saved = await service.SaveConfigAsync(noConfig.Id, new GubunConfigRequest("07:30", true));
        Assert.True(saved.IsSuccess);
        var items = (await service.GetAllAsync()).Data!.Items;
        Assert.Equal(["날씨 정보", "유가 정보"], items.Select(i => i.Name));
        Assert.Equal("07:30", items[0].SendTime);
        Assert.True(items[0].IsUse);
    }

    [Fact]
    public async Task AdminUser_CannotSuspendSelf()
    {
        using var context = new TestScope();
        User admin = context.AddUser("admin01", isAdmin: true);
        User otherAdmin = context.AddUser("admin02", isAdmin: true);
        var service = context.AdminUserService();
        Assert.Equal(ErrorCodes.CannotSuspendSelf,
            (await service.SetSuspensionAsync(admin.Id, admin.Id, new SuspensionRequest(true))).ErrorCode);
        Assert.True((await service.SetSuspensionAsync(admin.Id, otherAdmin.Id, new SuspensionRequest(true))).IsSuccess);
        Assert.Equal(ErrorCodes.NotFound,
            (await service.SetSuspensionAsync(admin.Id, 999, new SuspensionRequest(true))).ErrorCode);
        var page = await service.GetPageAsync(admin.Id, 1);
        Assert.True(page.Data!.Items.Single(u => u.Id == admin.Id).IsMe);
        Assert.True(page.Data.Items.Single(u => u.Id == otherAdmin.Id).IsSuspended);
    }

    [Fact]
    public async Task AdminUser_PagingTenPerPage()
    {
        using var context = new TestScope();
        for (int i = 0; i < 11; i++)
        {
            context.AddUser($"user{i:D4}");
        }
        var service = context.AdminUserService();
        var first = await service.GetPageAsync(1, 1);
        var second = await service.GetPageAsync(1, 2);
        Assert.Equal(10, first.Data!.Items.Count);
        Assert.Equal(2, first.Data.TotalPages);
        Assert.Single(second.Data!.Items);
        Assert.Equal(ErrorCodes.InvalidInput, (await service.GetPageAsync(1, 3)).ErrorCode);
    }
}
