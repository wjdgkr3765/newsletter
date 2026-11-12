using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using NewsletterApp.Data;
using NewsletterApp.Data.Entities;
using NewsletterApp.Models;
using NewsletterApp.Models.Responses;
using NewsletterApp.Services.Contents;

namespace NewsletterApp.Services;

// 뉴스레터 만들어 보내기, 다시 보내기, 오늘 상태 (docs/api.md 6.8, 7번)
public sealed class NewsletterSendService(
    AppDbContext db,
    IMailService mail,
    IUnsubscribeTokenService tokens,
    ISendLockService sendLocks,
    ContentBuilderResolver contentBuilders,
    IClock clock,
    IConfiguration configuration,
    ILogger<NewsletterSendService> logger)
{
    private sealed record Recipient(long UserId, string Email);

    public async Task<ServiceResult<TodayResponse>> GetTodayAsync(long gubunId)
    {
        if (gubunId <= 0)
        {
            return ServiceResult<TodayResponse>.Fail(ErrorCodes.InvalidInput);
        }
        if (!await db.Gubuns.AnyAsync(g => g.Id == gubunId))
        {
            return ServiceResult<TodayResponse>.Fail(ErrorCodes.NotFound);
        }
        DateOnly today = clock.KstToday;
        Newsletter? newsletter = await db.Newsletters.AsNoTracking()
            .SingleOrDefaultAsync(n => n.GubunId == gubunId && n.SendDay == today);
        bool isSending = sendLocks.IsLocked(gubunId);
        string sendDay = today.ToString("yyyy-MM-dd");
        if (newsletter is null)
        {
            int subscriberCount = await ActiveSubscribers(gubunId).CountAsync();
            return ServiceResult<TodayResponse>.Ok(
                new TodayResponse(false, sendDay, null, null, null, 0, subscriberCount, isSending));
        }
        int receivedCount = await db.Historys.CountAsync(h => h.NewsletterId == newsletter.Id);
        int pendingCount = await PendingSubscribers(gubunId, newsletter.Id).CountAsync();
        return ServiceResult<TodayResponse>.Ok(new TodayResponse(
            true, sendDay, newsletter.Id, newsletter.Title, newsletter.CreateDate, receivedCount, pendingCount, isSending));
    }

    // 오늘 뉴스레터를 만들어 구독한 정상 회원에게 보낸다. (자동 발송, 직접 발송 공통)
    public async Task<ServiceResult<SendResponse>> CreateAndSendAsync(long gubunId, CancellationToken cancellationToken)
    {
        if (gubunId <= 0)
        {
            return ServiceResult<SendResponse>.Fail(ErrorCodes.InvalidInput);
        }
        Gubun? gubun = await db.Gubuns.AsNoTracking().SingleOrDefaultAsync(g => g.Id == gubunId, cancellationToken);
        if (gubun is null)
        {
            return ServiceResult<SendResponse>.Fail(ErrorCodes.NotFound);
        }
        using IDisposable? sendLock = sendLocks.TryAcquire(gubunId);
        if (sendLock is null)
        {
            return ServiceResult<SendResponse>.Fail(ErrorCodes.SendingInProgress);
        }
        DateOnly today = clock.KstToday;
        if (await db.Newsletters.AnyAsync(n => n.GubunId == gubunId && n.SendDay == today, cancellationToken))
        {
            return ServiceResult<SendResponse>.Fail(ErrorCodes.NewsletterExists);
        }
        List<Recipient> recipients = await ActiveSubscribers(gubunId).ToListAsync(cancellationToken);
        if (recipients.Count == 0)
        {
            return ServiceResult<SendResponse>.Fail(ErrorCodes.NoSubscriber);
        }
        string? content = await contentBuilders.BuildAsync(gubun.Name, today, cancellationToken);
        if (content is null)
        {
            return ServiceResult<SendResponse>.Fail(ErrorCodes.ContentFetchFailed);
        }
        var newsletter = new Newsletter
        {
            Title = $"[{gubun.Name}] {today:yyyy-MM-dd}",
            Content = content,
            GubunId = gubunId,
            SendDay = today,
            CreateDate = clock.UtcNow
        };
        db.Newsletters.Add(newsletter);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ServiceResult<SendResponse>.Fail(ErrorCodes.NewsletterExists);
        }
        int receivedCount = await SendToAsync(newsletter, recipients, cancellationToken);
        if (receivedCount == 0)
        {
            // 아무도 받지 못해도 뉴스레터는 남긴다. 그날은 다시 보내기로만 보낼 수 있다. (D-005)
            return ServiceResult<SendResponse>.Fail(ErrorCodes.SendFailed);
        }
        return ServiceResult<SendResponse>.Ok(
            new SendResponse(newsletter.Id, receivedCount, recipients.Count - receivedCount));
    }

    // 오늘 뉴스레터를 받지 않은 구독자에게 보낸다.
    public async Task<ServiceResult<ResendResponse>> ResendAsync(long gubunId, CancellationToken cancellationToken)
    {
        if (gubunId <= 0)
        {
            return ServiceResult<ResendResponse>.Fail(ErrorCodes.InvalidInput);
        }
        if (!await db.Gubuns.AnyAsync(g => g.Id == gubunId, cancellationToken))
        {
            return ServiceResult<ResendResponse>.Fail(ErrorCodes.NotFound);
        }
        using IDisposable? sendLock = sendLocks.TryAcquire(gubunId);
        if (sendLock is null)
        {
            return ServiceResult<ResendResponse>.Fail(ErrorCodes.SendingInProgress);
        }
        DateOnly today = clock.KstToday;
        Newsletter? newsletter = await db.Newsletters
            .SingleOrDefaultAsync(n => n.GubunId == gubunId && n.SendDay == today, cancellationToken);
        if (newsletter is null)
        {
            return ServiceResult<ResendResponse>.Fail(ErrorCodes.NotFound);
        }
        List<Recipient> recipients = await PendingSubscribers(gubunId, newsletter.Id).ToListAsync(cancellationToken);
        if (recipients.Count == 0)
        {
            return ServiceResult<ResendResponse>.Fail(ErrorCodes.NoPendingSubscriber);
        }
        int receivedCount = await SendToAsync(newsletter, recipients, cancellationToken);
        if (receivedCount == 0)
        {
            return ServiceResult<ResendResponse>.Fail(ErrorCodes.SendFailed);
        }
        return ServiceResult<ResendResponse>.Ok(new ResendResponse(receivedCount, recipients.Count - receivedCount));
    }

    // 자동 발송: 사용 여부가 "사용"이고 발송 시간이 sendTime인 종류를 보낸다.
    public async Task SendScheduledAsync(TimeOnly sendTime, CancellationToken cancellationToken)
    {
        List<long> gubunIds = await db.NewsletterConfigs.AsNoTracking()
            .Where(c => c.IsUse && c.SendTime == sendTime)
            .Select(c => c.GubunId)
            .ToListAsync(cancellationToken);
        foreach (long gubunId in gubunIds)
        {
            try
            {
                ServiceResult<SendResponse> result = await CreateAndSendAsync(gubunId, cancellationToken);
                logger.LogInformation("자동 발송 종류 {GubunId}: {Result}", gubunId,
                    result.IsSuccess ? $"받은 회원 {result.Data!.ReceivedCount}명" : result.ErrorCode);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "자동 발송 실패 종류 {GubunId}", gubunId);
            }
            db.ChangeTracker.Clear();
        }
    }

    private IQueryable<Recipient> ActiveSubscribers(long gubunId) =>
        db.Subscriptions.AsNoTracking()
            .Where(s => s.GubunId == gubunId && !s.User!.IsSuspended)
            .Select(s => new Recipient(s.UserId, s.User!.Email));

    private IQueryable<Recipient> PendingSubscribers(long gubunId, long newsletterId) =>
        db.Subscriptions.AsNoTracking()
            .Where(s => s.GubunId == gubunId
                && !s.User!.IsSuspended
                && !db.Historys.Any(h => h.NewsletterId == newsletterId && h.UserId == s.UserId))
            .Select(s => new Recipient(s.UserId, s.User!.Email));

    // 회원마다 한 통씩 보내고, 받는 데 성공한 회원만 수신 내역에 남긴다. 성공한 수를 돌려준다.
    private async Task<int> SendToAsync(Newsletter newsletter, List<Recipient> recipients, CancellationToken cancellationToken)
    {
        IMailBatch batch;
        try
        {
            batch = await mail.OpenBatchAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError("메일 서버 연결 실패: {Type}", exception.GetType().Name);
            return 0;
        }
        int receivedCount = 0;
        await using (batch)
        {
            foreach (Recipient recipient in recipients)
            {
                string html = newsletter.Content + UnsubscribeFooter(recipient.UserId, newsletter.GubunId);
                if (!await batch.TrySendAsync(recipient.Email, newsletter.Title, html, cancellationToken))
                {
                    continue;
                }
                db.Historys.Add(new History
                {
                    NewsletterId = newsletter.Id,
                    UserId = recipient.UserId,
                    CreateDate = clock.UtcNow
                });
                await db.SaveChangesAsync(cancellationToken);
                receivedCount++;
            }
        }
        return receivedCount;
    }

    private string UnsubscribeFooter(long userId, long gubunId)
    {
        string baseUrl = (configuration["App:BaseUrl"] ?? "").TrimEnd('/');
        string url = $"{baseUrl}/unsubscribe?token={Uri.EscapeDataString(tokens.Create(userId, gubunId))}";
        string href = HtmlEncoder.Default.Encode(url);
        return "<hr><p style=\"color:#6c757d;font-size:13px\">더 이상 받지 않으려면 수신 거부를 눌러 주세요. "
            + $"<a href=\"{href}\">수신 거부</a></p>";
    }
}
