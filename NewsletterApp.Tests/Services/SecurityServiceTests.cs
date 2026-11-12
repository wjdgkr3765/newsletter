using NewsletterApp.Services;

namespace NewsletterApp.Tests.Services;

// 인증 코드, 로그인 잠금, 비밀번호 해시, 수신 거부 토큰, 본문 인코딩
public sealed class SecurityServiceTests
{
    [Fact]
    public void DefaultContent_EncodesGubunName()
    {
        string html = new NewsletterApp.Services.Contents.DefaultContentBuilder().Build("<img src=x onerror=a>", new DateOnly(2026, 11, 12));
        Assert.DoesNotContain("<img", html);
        Assert.Contains("&lt;img", html);
    }

    [Fact]
    public void Code_CorrectCode_Verified()
    {
        using var context = new TestScope();
        string code = context.Codes.Issue("k");
        Assert.Equal(CodeCheckStatus.Ok, context.Codes.Verify("k", code).Status);
        Assert.True(context.Codes.IsVerified("k"));
    }

    [Fact]
    public void Code_FourWrongThenCorrect_Verified()
    {
        using var context = new TestScope();
        string code = context.Codes.Issue("k");
        string wrong = code == "000000" ? "111111" : "000000";
        for (int i = 1; i <= 4; i++)
        {
            CodeCheckResult result = context.Codes.Verify("k", wrong);
            Assert.Equal(CodeCheckStatus.Invalid, result.Status);
            Assert.Equal(5 - i, result.RemainingAttempts);
        }
        Assert.Equal(CodeCheckStatus.Ok, context.Codes.Verify("k", code).Status);
    }

    [Fact]
    public void Code_FiveWrong_CannotUse()
    {
        using var context = new TestScope();
        string code = context.Codes.Issue("k");
        string wrong = code == "000000" ? "111111" : "000000";
        for (int i = 0; i < 5; i++)
        {
            context.Codes.Verify("k", wrong);
        }
        Assert.Equal(CodeCheckStatus.Expired, context.Codes.Verify("k", code).Status);
        Assert.False(context.Codes.IsVerified("k"));
    }

    [Fact]
    public void Code_ExpiresAfterTenMinutes()
    {
        using var context = new TestScope();
        string code = context.Codes.Issue("a");
        context.Codes.Issue("b");
        context.Clock.Advance(TimeSpan.FromSeconds(599));
        Assert.Equal(CodeCheckStatus.Ok, context.Codes.Verify("a", code).Status);
        context.Clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(CodeCheckStatus.Expired, context.Codes.Verify("b", "123456").Status);
    }

    [Fact]
    public void Code_ReissueInvalidatesOldCode()
    {
        using var context = new TestScope();
        string oldCode = context.Codes.Issue("k");
        string newCode;
        do
        {
            newCode = context.Codes.Issue("k");
        }
        while (newCode == oldCode);
        Assert.Equal(CodeCheckStatus.Invalid, context.Codes.Verify("k", oldCode).Status);
        Assert.Equal(CodeCheckStatus.Ok, context.Codes.Verify("k", newCode).Status);
    }

    [Fact]
    public void Code_VerifiedStateLastsThirtyMinutes()
    {
        using var context = new TestScope();
        string code = context.Codes.Issue("k");
        context.Codes.Verify("k", code);
        context.Clock.Advance(TimeSpan.FromMinutes(29));
        Assert.True(context.Codes.IsVerified("k"));
        context.Clock.Advance(TimeSpan.FromMinutes(1));
        Assert.False(context.Codes.IsVerified("k"));
    }

    [Fact]
    public void LoginAttempt_LocksAfterFiveFailures()
    {
        using var context = new TestScope();
        for (int i = 0; i < 4; i++)
        {
            context.LoginAttempts.RecordFailure("hong1234");
        }
        Assert.False(context.LoginAttempts.IsLocked("hong1234"));
        context.LoginAttempts.RecordFailure("hong1234");
        Assert.True(context.LoginAttempts.IsLocked("hong1234"));
        context.Clock.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromSeconds(1));
        Assert.True(context.LoginAttempts.IsLocked("hong1234"));
        context.Clock.Advance(TimeSpan.FromSeconds(1));
        Assert.False(context.LoginAttempts.IsLocked("hong1234"));
    }

    [Fact]
    public void LoginAttempt_ResetClearsFailures()
    {
        using var context = new TestScope();
        for (int i = 0; i < 4; i++)
        {
            context.LoginAttempts.RecordFailure("hong1234");
        }
        context.LoginAttempts.Reset("hong1234");
        context.LoginAttempts.RecordFailure("hong1234");
        Assert.False(context.LoginAttempts.IsLocked("hong1234"));
    }

    [Fact]
    public void PasswordHasher_UsesBcrypt()
    {
        var hasher = new PasswordHasher();
        string hash = hasher.Hash("password1!");
        Assert.StartsWith("$2", hash);
        Assert.Equal(60, hash.Length);
        Assert.True(hasher.Verify("password1!", hash));
        Assert.False(hasher.Verify("password2!", hash));
    }

    [Fact]
    public void UnsubscribeToken_RoundTripAndTamper()
    {
        using var context = new TestScope();
        string token = context.Tokens.Create(3, 7);
        Assert.Equal(new UnsubscribeTarget(3, 7), context.Tokens.Read(token));
        string tampered = token[..10] + (token[10] == 'A' ? 'B' : 'A') + token[11..];
        Assert.Null(context.Tokens.Read(tampered));
        Assert.Null(context.Tokens.Read("3:7"));
    }
}
