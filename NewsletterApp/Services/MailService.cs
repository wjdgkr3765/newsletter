using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NewsletterApp.Services;

// 여러 통을 한 연결로 보낼 때 쓴다. 한 통이 실패해도 다음 통은 계속 보낸다.
public interface IMailBatch : IAsyncDisposable
{
    Task<bool> TrySendAsync(string to, string subject, string html, CancellationToken cancellationToken);
}

public interface IMailService
{
    // 한 통을 보낸다. 실패하면 false.
    Task<bool> TrySendAsync(string to, string subject, string html, CancellationToken cancellationToken);

    // 연결에 실패하면 예외를 던진다.
    Task<IMailBatch> OpenBatchAsync(CancellationToken cancellationToken);
}

// Gmail SMTP 발송. 주소와 앱 비밀번호는 .env(Gmail__Address, Gmail__AppPassword)에서 읽는다.
public sealed class MailService(IConfiguration configuration, ILogger<MailService> logger) : IMailService
{
    private const string Host = "smtp.gmail.com";
    private const int Port = 587;

    public async Task<bool> TrySendAsync(string to, string subject, string html, CancellationToken cancellationToken)
    {
        try
        {
            await using IMailBatch batch = await OpenBatchAsync(cancellationToken);
            return await batch.TrySendAsync(to, subject, html, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError("메일 서버 연결 실패: {Type}", exception.GetType().Name);
            return false;
        }
    }

    public async Task<IMailBatch> OpenBatchAsync(CancellationToken cancellationToken)
    {
        string address = configuration["Gmail:Address"] ?? "";
        string password = configuration["Gmail:AppPassword"] ?? "";
        var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(Host, Port, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(address, password, cancellationToken);
        }
        catch
        {
            client.Dispose();
            throw;
        }
        return new MailBatch(client, address, logger);
    }

    private sealed class MailBatch(SmtpClient client, string from, ILogger logger) : IMailBatch
    {
        public async Task<bool> TrySendAsync(string to, string subject, string html, CancellationToken cancellationToken)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(from));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;
                message.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();
                await client.SendAsync(FormatOptions.Default, new SendRequest(message), cancellationToken);
                return true;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning("메일 한 통 발송 실패: {Type}", exception.GetType().Name);
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning("메일 서버 연결 종료 실패: {Type}", exception.GetType().Name);
            }
            client.Dispose();
        }
    }
}
