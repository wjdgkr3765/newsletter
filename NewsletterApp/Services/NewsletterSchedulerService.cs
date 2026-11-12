namespace NewsletterApp.Services;

// 자동 발송: 매 분이 바뀔 때 그 분(한국 시간)이 발송 시간인 종류를 보낸다.
// 깨어난 시각이 그 분을 넘겼으면 건너뛴다(밀린 분을 따라잡지 않는다).
// 발송이 길어져도 다음 분 확인이 늦지 않도록 분마다 따로 실행한다.
public sealed class NewsletterSchedulerService(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    ILogger<NewsletterSchedulerService> logger) : BackgroundService
{
    // 타이머가 분 경계보다 조금 일찍 깨는 경우를 피하기 위한 여유
    private static readonly TimeSpan WakeMargin = TimeSpan.FromMilliseconds(200);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime now = clock.UtcNow;
            DateTime nextMinute = TruncateToMinute(now).AddMinutes(1);
            await Task.Delay(nextMinute - now + WakeMargin, stoppingToken);

            if (TruncateToMinute(clock.UtcNow) != nextMinute)
            {
                logger.LogWarning("자동 발송 확인이 늦어 {Minute}(UTC) 분을 건너뜁니다.", nextMinute);
                continue;
            }
            DateTime kstMinute = TruncateToMinute(KstClock.ToKst(nextMinute));
            _ = RunMinuteAsync(new TimeOnly(kstMinute.Hour, kstMinute.Minute), stoppingToken);
        }
    }

    private async Task RunMinuteAsync(TimeOnly sendTime, CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            var sendService = scope.ServiceProvider.GetRequiredService<NewsletterSendService>();
            await sendService.SendScheduledAsync(sendTime, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // 앱 종료
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "자동 발송 확인 실패 {SendTime}", sendTime);
        }
    }

    private static DateTime TruncateToMinute(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
}
