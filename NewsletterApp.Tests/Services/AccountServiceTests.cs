using Microsoft.EntityFrameworkCore;
using NewsletterApp.Data.Entities;
using NewsletterApp.Models;
using NewsletterApp.Models.Requests;
using NewsletterApp.Services;

namespace NewsletterApp.Tests.Services;

// 회원가입, 로그인, 비밀번호 찾기, 내 정보, 탈퇴
public sealed class AccountServiceTests
{
    private static string LastCode(TestScope context) =>
        System.Text.RegularExpressions.Regex.Match(context.Mail.Sent[^1].Html, "<strong>(\\d{6})</strong>").Groups[1].Value;

    [Fact]
    public async Task Register_WithoutVerification_Fails()
    {
        using var context = new TestScope();
        var result = await context.AuthService().RegisterAsync(
            new RegisterRequest("hong1234", "password1!", "password1!", "hong@example.com", "홍길동"));
        Assert.Equal(ErrorCodes.EmailNotVerified, result.ErrorCode);
    }

    [Fact]
    public async Task Register_AfterVerification_CreatesNormalUser()
    {
        using var context = new TestScope();
        AuthService auth = context.AuthService();
        var sent = await auth.SendSignupCodeAsync(new EmailRequest(" Hong@Example.com "), CancellationToken.None);
        Assert.True(sent.IsSuccess);
        Assert.Equal(600, sent.Data!.ExpiresInSeconds);
        Assert.Equal("hong@example.com", context.Mail.Sent[^1].To);
        Assert.True(auth.VerifySignupCode(new EmailCodeVerifyRequest("hong@example.com", LastCode(context))).IsSuccess);

        var result = await auth.RegisterAsync(
            new RegisterRequest("hong1234", "password1!", "password1!", "HONG@example.com", " 홍길동 "));

        Assert.True(result.IsSuccess);
        User user = await context.CreateDb().Users.SingleAsync();
        Assert.Equal("hong@example.com", user.Email);
        Assert.Equal("홍길동", user.Name);
        Assert.False(user.IsAdmin);
        Assert.False(user.IsSuspended);
        Assert.False(context.Codes.IsVerified(VerificationCodeService.SignupKey("hong@example.com")));
    }

    [Fact]
    public async Task Register_InvalidInput_Fails()
    {
        using var context = new TestScope();
        AuthService auth = context.AuthService();
        var mismatch = await auth.RegisterAsync(
            new RegisterRequest("hong1234", "password1!", "password2!", "hong@example.com", "홍길동"));
        var badId = await auth.RegisterAsync(
            new RegisterRequest("Hong", "password1!", "password1!", "hong@example.com", "홍길동"));
        Assert.Equal(ErrorCodes.InvalidInput, mismatch.ErrorCode);
        Assert.Equal(ErrorCodes.InvalidInput, badId.ErrorCode);
    }

    [Fact]
    public async Task SignupCode_DuplicateEmail_Fails()
    {
        using var context = new TestScope();
        context.AddUser("hong1234");
        var result = await context.AuthService().SendSignupCodeAsync(
            new EmailRequest("hong1234@example.com"), CancellationToken.None);
        Assert.Equal(ErrorCodes.DuplicateEmail, result.ErrorCode);
        Assert.Empty(context.Mail.Sent);
    }

    [Fact]
    public async Task SignupCode_MailFails_ReturnsMailSendFailed()
    {
        using var context = new TestScope();
        context.Mail.IsConnectFailing = true;
        var result = await context.AuthService().SendSignupCodeAsync(
            new EmailRequest("hong@example.com"), CancellationToken.None);
        Assert.Equal(ErrorCodes.MailSendFailed, result.ErrorCode);
    }

    [Fact]
    public async Task Availability_ChecksLoginIdAndEmail()
    {
        using var context = new TestScope();
        context.AddUser("hong1234");
        AuthService auth = context.AuthService();
        Assert.False((await auth.CheckAvailabilityAsync("hong1234", null)).Data!.Available);
        Assert.True((await auth.CheckAvailabilityAsync("kim12345", null)).Data!.Available);
        Assert.False((await auth.CheckAvailabilityAsync(null, "HONG1234@example.com")).Data!.Available);
        Assert.Equal(ErrorCodes.InvalidInput, (await auth.CheckAvailabilityAsync("hong1234", "a@b.co")).ErrorCode);
        Assert.Equal(ErrorCodes.InvalidInput, (await auth.CheckAvailabilityAsync("ab", null)).ErrorCode);
    }

    [Fact]
    public async Task Login_WrongIdWrongPasswordSuspended_SameError()
    {
        using var context = new TestScope();
        context.AddUser("hong1234");
        context.AddUser("kim12345", isSuspended: true);
        AuthService auth = context.AuthService();
        Assert.Equal(ErrorCodes.LoginFailed, (await auth.LoginAsync(new LoginRequest("nouser11", "password1!"))).ErrorCode);
        Assert.Equal(ErrorCodes.LoginFailed, (await auth.LoginAsync(new LoginRequest("hong1234", "wrong"))).ErrorCode);
        Assert.Equal(ErrorCodes.LoginFailed, (await auth.LoginAsync(new LoginRequest("kim12345", "password1!"))).ErrorCode);
        Assert.True((await auth.LoginAsync(new LoginRequest("hong1234", "password1!"))).IsSuccess);
    }

    [Fact]
    public async Task Login_FiveFailures_LockedEvenWithCorrectPassword()
    {
        using var context = new TestScope();
        context.AddUser("hong1234");
        AuthService auth = context.AuthService();
        for (int i = 0; i < 5; i++)
        {
            await auth.LoginAsync(new LoginRequest("hong1234", "wrong"));
        }
        Assert.Equal(ErrorCodes.LoginLocked, (await auth.LoginAsync(new LoginRequest("hong1234", "password1!"))).ErrorCode);
        context.Clock.Advance(TimeSpan.FromMinutes(10));
        Assert.True((await auth.LoginAsync(new LoginRequest("hong1234", "password1!"))).IsSuccess);
    }

    [Fact]
    public async Task PasswordReset_FullFlow()
    {
        using var context = new TestScope();
        context.AddUser("hong1234");
        AuthService auth = context.AuthService();
        var mismatch = await auth.SendPasswordResetCodeAsync(
            new PasswordResetCodeRequest("hong1234", "other@example.com"), CancellationToken.None);
        Assert.Equal(ErrorCodes.MemberNotMatched, mismatch.ErrorCode);
        Assert.Empty(context.Mail.Sent);

        await auth.SendPasswordResetCodeAsync(
            new PasswordResetCodeRequest("hong1234", "hong1234@example.com"), CancellationToken.None);
        var beforeVerify = await auth.ResetPasswordAsync(
            new PasswordResetRequest("hong1234", "hong1234@example.com", "newpass1!", "newpass1!"));
        Assert.Equal(ErrorCodes.EmailNotVerified, beforeVerify.ErrorCode);

        var verified = await auth.VerifyPasswordResetCodeAsync(
            new PasswordResetVerifyRequest("hong1234", "hong1234@example.com", LastCode(context)));
        Assert.True(verified.IsSuccess);
        var reset = await auth.ResetPasswordAsync(
            new PasswordResetRequest("hong1234", "hong1234@example.com", "newpass1!", "newpass1!"));
        Assert.True(reset.IsSuccess);
        Assert.Equal("/login", reset.Data!.RedirectUrl);
        Assert.True((await auth.LoginAsync(new LoginRequest("hong1234", "newpass1!"))).IsSuccess);
    }

    [Fact]
    public async Task PasswordReset_SuspendedMember_NotMatched()
    {
        using var context = new TestScope();
        context.AddUser("hong1234", isSuspended: true);
        var result = await context.AuthService().SendPasswordResetCodeAsync(
            new PasswordResetCodeRequest("hong1234", "hong1234@example.com"), CancellationToken.None);
        Assert.Equal(ErrorCodes.MemberNotMatched, result.ErrorCode);
    }

    [Fact]
    public async Task Profile_SameEmail_SavesNameWithoutVerification()
    {
        using var context = new TestScope();
        User user = context.AddUser("hong1234");
        var result = await context.MeService().UpdateProfileAsync(user.Id, new ProfileRequest("새이름", "hong1234@example.com"));
        Assert.True(result.IsSuccess);
        Assert.Equal("새이름", (await context.CreateDb().Users.SingleAsync()).Name);
    }

    [Fact]
    public async Task Profile_NewEmail_RequiresVerification()
    {
        using var context = new TestScope();
        User user = context.AddUser("hong1234");
        context.AddUser("kim12345");
        MeService me = context.MeService();
        Assert.Equal(ErrorCodes.EmailNotVerified,
            (await me.UpdateProfileAsync(user.Id, new ProfileRequest("홍길동", "new@example.com"))).ErrorCode);
        Assert.Equal(ErrorCodes.SameEmail,
            (await me.SendEmailChangeCodeAsync(user.Id, new EmailRequest("hong1234@example.com"), CancellationToken.None)).ErrorCode);
        Assert.Equal(ErrorCodes.DuplicateEmail,
            (await me.SendEmailChangeCodeAsync(user.Id, new EmailRequest("kim12345@example.com"), CancellationToken.None)).ErrorCode);

        await me.SendEmailChangeCodeAsync(user.Id, new EmailRequest("new@example.com"), CancellationToken.None);
        Assert.True(me.VerifyEmailChangeCode(user.Id, new EmailCodeVerifyRequest("new@example.com", LastCode(context))).IsSuccess);
        var result = await me.UpdateProfileAsync(user.Id, new ProfileRequest("홍길동", "new@example.com"));
        Assert.True(result.IsSuccess);
        Assert.Equal("new@example.com", (await context.CreateDb().Users.SingleAsync(u => u.Id == user.Id)).Email);
    }

    [Fact]
    public async Task ChangePassword_ChecksCurrentPassword()
    {
        using var context = new TestScope();
        User user = context.AddUser("hong1234");
        MeService me = context.MeService();
        Assert.Equal(ErrorCodes.WrongPassword,
            (await me.ChangePasswordAsync(user.Id, new PasswordChangeRequest("wrong", "newpass1!", "newpass1!"))).ErrorCode);
        Assert.True((await me.ChangePasswordAsync(user.Id, new PasswordChangeRequest("password1!", "newpass1!", "newpass1!"))).IsSuccess);
    }

    [Fact]
    public async Task Withdraw_DeletesUserDataKeepsNewsletter()
    {
        using var context = new TestScope();
        User user = context.AddUser("hong1234");
        Gubun gubun = context.AddGubun("유가 정보");
        context.Subscribe(user, gubun);
        await context.SendService().CreateAndSendAsync(gubun.Id, CancellationToken.None);

        Assert.Equal(ErrorCodes.WrongPassword,
            (await context.MeService().WithdrawAsync(user.Id, new WithdrawRequest("wrong"))).ErrorCode);
        var result = await context.MeService().WithdrawAsync(user.Id, new WithdrawRequest("password1!"));

        Assert.True(result.IsSuccess);
        await using var db = context.CreateDb();
        Assert.Empty(db.Users);
        Assert.Empty(db.Subscriptions);
        Assert.Empty(db.Historys);
        Assert.Single(db.Newsletters);
    }

    [Fact]
    public async Task Withdraw_Admin_Rejected()
    {
        using var context = new TestScope();
        User admin = context.AddUser("admin01", isAdmin: true);
        var result = await context.MeService().WithdrawAsync(admin.Id, new WithdrawRequest("password1!"));
        Assert.Equal(ErrorCodes.AdminCannotWithdraw, result.ErrorCode);
    }
}
