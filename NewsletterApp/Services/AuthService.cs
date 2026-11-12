using Microsoft.EntityFrameworkCore;
using NewsletterApp.Data;
using NewsletterApp.Data.Entities;
using NewsletterApp.Models;
using NewsletterApp.Models.Requests;
using NewsletterApp.Models.Responses;

namespace NewsletterApp.Services;

// 회원가입, 로그인, 비밀번호 찾기 (docs/api.md 6.1)
public sealed class AuthService(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IVerificationCodeService codes,
    ILoginAttemptService loginAttempts,
    IMailService mail,
    IClock clock)
{
    public async Task<ServiceResult<AvailabilityResponse>> CheckAvailabilityAsync(string? loginId, string? email)
    {
        bool hasLoginId = loginId is not null;
        bool hasEmail = email is not null;
        if (hasLoginId == hasEmail)
        {
            return ServiceResult<AvailabilityResponse>.Fail(ErrorCodes.InvalidInput);
        }
        if (hasLoginId)
        {
            string value = InputRules.Trim(loginId);
            if (!InputRules.IsLoginId(value))
            {
                return ServiceResult<AvailabilityResponse>.Fail(ErrorCodes.InvalidInput);
            }
            bool isUsed = await db.Users.AnyAsync(u => u.LoginId == value);
            return ServiceResult<AvailabilityResponse>.Ok(new AvailabilityResponse(!isUsed));
        }
        string normalized = InputRules.NormalizeEmail(email);
        if (!InputRules.IsEmail(normalized))
        {
            return ServiceResult<AvailabilityResponse>.Fail(ErrorCodes.InvalidInput);
        }
        bool isEmailUsed = await db.Users.AnyAsync(u => u.Email == normalized);
        return ServiceResult<AvailabilityResponse>.Ok(new AvailabilityResponse(!isEmailUsed));
    }

    public async Task<ServiceResult<ExpiresResponse>> SendSignupCodeAsync(EmailRequest request, CancellationToken cancellationToken)
    {
        string email = InputRules.NormalizeEmail(request.Email);
        if (!InputRules.IsEmail(email))
        {
            return ServiceResult<ExpiresResponse>.Fail(ErrorCodes.InvalidInput);
        }
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return ServiceResult<ExpiresResponse>.Fail(ErrorCodes.DuplicateEmail);
        }
        string key = VerificationCodeService.SignupKey(email);
        return await SendCodeAsync(key, email, cancellationToken);
    }

    public ServiceResult<EmptyData> VerifySignupCode(EmailCodeVerifyRequest request)
    {
        string email = InputRules.NormalizeEmail(request.Email);
        if (!InputRules.IsEmail(email) || !InputRules.IsCode(request.Code))
        {
            return ServiceResult<EmptyData>.Fail(ErrorCodes.InvalidInput);
        }
        return CheckCode(codes, VerificationCodeService.SignupKey(email), request.Code!);
    }

    public async Task<ServiceResult<EmptyData>> RegisterAsync(RegisterRequest request)
    {
        string loginId = InputRules.Trim(request.LoginId);
        string email = InputRules.NormalizeEmail(request.Email);
        string name = InputRules.Trim(request.Name);
        if (!InputRules.IsLoginId(loginId)
            || !InputRules.IsPassword(request.Password)
            || request.Password != request.PasswordConfirm
            || !InputRules.IsEmail(email)
            || !InputRules.IsUserName(name))
        {
            return ServiceResult<EmptyData>.Fail(ErrorCodes.InvalidInput);
        }
        string key = VerificationCodeService.SignupKey(email);
        if (!codes.IsVerified(key))
        {
            return ServiceResult<EmptyData>.Fail(ErrorCodes.EmailNotVerified);
        }
        if (await db.Users.AnyAsync(u => u.LoginId == loginId))
        {
            return ServiceResult<EmptyData>.Fail(ErrorCodes.DuplicateLoginId);
        }
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return ServiceResult<EmptyData>.Fail(ErrorCodes.DuplicateEmail);
        }
        db.Users.Add(new User
        {
            LoginId = loginId,
            PasswordHash = passwordHasher.Hash(request.Password!),
            Email = email,
            Name = name,
            CreateDate = clock.UtcNow
        });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // 동시에 같은 아이디·이메일로 가입한 경우 (UNIQUE 제약)
            bool isLoginIdUsed = await db.Users.AsNoTracking().AnyAsync(u => u.LoginId == loginId);
            return ServiceResult<EmptyData>.Fail(isLoginIdUsed ? ErrorCodes.DuplicateLoginId : ErrorCodes.DuplicateEmail);
        }
        codes.Consume(key);
        return ServiceResult<EmptyData>.Ok(new EmptyData());
    }

    // 성공하면 로그인할 회원을 돌려준다. 실패 사유는 모두 LOGIN_FAILED로 같게 알린다.
    public async Task<ServiceResult<User>> LoginAsync(LoginRequest request)
    {
        string loginId = InputRules.Trim(request.LoginId);
        if (loginId.Length == 0 || loginId.Length > 20 || !InputRules.IsPasswordInput(request.Password))
        {
            return ServiceResult<User>.Fail(ErrorCodes.InvalidInput);
        }
        if (loginAttempts.IsLocked(loginId))
        {
            return ServiceResult<User>.Fail(ErrorCodes.LoginLocked);
        }
        User? user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.LoginId == loginId);
        if (user is null || user.IsSuspended || !passwordHasher.Verify(request.Password!, user.PasswordHash))
        {
            loginAttempts.RecordFailure(loginId);
            return ServiceResult<User>.Fail(ErrorCodes.LoginFailed);
        }
        loginAttempts.Reset(loginId);
        return ServiceResult<User>.Ok(user);
    }

    public async Task<ServiceResult<ExpiresResponse>> SendPasswordResetCodeAsync(
        PasswordResetCodeRequest request, CancellationToken cancellationToken)
    {
        var (error, user) = await FindResetMemberAsync(request.LoginId, request.Email);
        if (error is not null)
        {
            return ServiceResult<ExpiresResponse>.Fail(error);
        }
        return await SendCodeAsync(VerificationCodeService.PasswordResetKey(user!.Id), user.Email, cancellationToken);
    }

    public async Task<ServiceResult<EmptyData>> VerifyPasswordResetCodeAsync(PasswordResetVerifyRequest request)
    {
        if (!InputRules.IsCode(request.Code))
        {
            return ServiceResult<EmptyData>.Fail(ErrorCodes.InvalidInput);
        }
        var (error, user) = await FindResetMemberAsync(request.LoginId, request.Email);
        if (error is not null)
        {
            return ServiceResult<EmptyData>.Fail(error);
        }
        return CheckCode(codes, VerificationCodeService.PasswordResetKey(user!.Id), request.Code!);
    }

    public async Task<ServiceResult<RedirectResponse>> ResetPasswordAsync(PasswordResetRequest request)
    {
        if (!InputRules.IsPassword(request.NewPassword) || request.NewPassword != request.NewPasswordConfirm)
        {
            return ServiceResult<RedirectResponse>.Fail(ErrorCodes.InvalidInput);
        }
        var (error, user) = await FindResetMemberAsync(request.LoginId, request.Email);
        if (error is not null)
        {
            return ServiceResult<RedirectResponse>.Fail(error);
        }
        string key = VerificationCodeService.PasswordResetKey(user!.Id);
        if (!codes.IsVerified(key))
        {
            return ServiceResult<RedirectResponse>.Fail(ErrorCodes.EmailNotVerified);
        }
        User tracked = await db.Users.SingleAsync(u => u.Id == user.Id);
        tracked.PasswordHash = passwordHasher.Hash(request.NewPassword!);
        await db.SaveChangesAsync();
        codes.Consume(key);
        return ServiceResult<RedirectResponse>.Ok(new RedirectResponse("/login"));
    }

    // 인증 코드 확인 결과를 오류 코드로 바꾼다. (회원가입·비밀번호 찾기·이메일 변경 공통)
    public static ServiceResult<EmptyData> CheckCode(IVerificationCodeService codes, string key, string code)
    {
        CodeCheckResult result = codes.Verify(key, code);
        return result.Status switch
        {
            CodeCheckStatus.Ok => ServiceResult<EmptyData>.Ok(new EmptyData()),
            CodeCheckStatus.Invalid => ServiceResult<EmptyData>.Fail(ErrorCodes.CodeInvalid,
                $"인증 코드가 맞지 않습니다. (남은 횟수 {result.RemainingAttempts}번)"),
            _ => ServiceResult<EmptyData>.Fail(ErrorCodes.CodeExpired)
        };
    }

    // 인증 코드를 만들어 메일로 보낸다. (회원가입·비밀번호 찾기·이메일 변경 공통)
    public static async Task<ServiceResult<ExpiresResponse>> SendCodeAsync(
        IVerificationCodeService codes, IMailService mail, string key, string to, CancellationToken cancellationToken)
    {
        string code = codes.Issue(key);
        string html = $"<p>인증 코드: <strong>{code}</strong></p><p>10분 동안 유효합니다.</p>";
        bool isSent = await mail.TrySendAsync(to, "[뉴스레터] 인증 코드", html, cancellationToken);
        if (!isSent)
        {
            codes.Consume(key);
            return ServiceResult<ExpiresResponse>.Fail(ErrorCodes.MailSendFailed);
        }
        return ServiceResult<ExpiresResponse>.Ok(new ExpiresResponse(VerificationCodeService.ExpiresInSeconds));
    }

    private Task<ServiceResult<ExpiresResponse>> SendCodeAsync(string key, string to, CancellationToken cancellationToken) =>
        SendCodeAsync(codes, mail, key, to, cancellationToken);

    // 아이디와 이메일이 같은 정상 회원을 찾는다. 없거나 정지 회원이면 MEMBER_NOT_MATCHED.
    private async Task<(string? Error, User? User)> FindResetMemberAsync(string? loginIdInput, string? emailInput)
    {
        string loginId = InputRules.Trim(loginIdInput);
        string email = InputRules.NormalizeEmail(emailInput);
        if (!InputRules.IsLoginId(loginId) || !InputRules.IsEmail(email))
        {
            return (ErrorCodes.InvalidInput, null);
        }
        User? user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.LoginId == loginId && u.Email == email && !u.IsSuspended);
        return user is null ? (ErrorCodes.MemberNotMatched, null) : (null, user);
    }
}
