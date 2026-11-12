using Microsoft.EntityFrameworkCore;
using NewsletterApp.Data;
using NewsletterApp.Data.Entities;
using NewsletterApp.Models;
using NewsletterApp.Models.Requests;
using NewsletterApp.Models.Responses;

namespace NewsletterApp.Services;

// 내 정보 수정, 비밀번호 변경, 탈퇴 (docs/api.md 6.2). 대상은 항상 로그인한 회원 자신이다.
public sealed class MeService(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IVerificationCodeService codes,
    IMailService mail)
{
    public async Task<ServiceResult<MeResponse>> GetAsync(long userId)
    {
        User? user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return ServiceResult<MeResponse>.Fail(ErrorCodes.NotFound);
        }
        return ServiceResult<MeResponse>.Ok(
            new MeResponse(user.LoginId, user.Name, user.Email, user.IsAdmin, user.CreateDate));
    }

    public async Task<ServiceResult<ExpiresResponse>> SendEmailChangeCodeAsync(
        long userId, EmailRequest request, CancellationToken cancellationToken)
    {
        string email = InputRules.NormalizeEmail(request.Email);
        if (!InputRules.IsEmail(email))
        {
            return ServiceResult<ExpiresResponse>.Fail(ErrorCodes.InvalidInput);
        }
        User? user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<ExpiresResponse>.Fail(ErrorCodes.NotFound);
        }
        if (user.Email == email)
        {
            return ServiceResult<ExpiresResponse>.Fail(ErrorCodes.SameEmail);
        }
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return ServiceResult<ExpiresResponse>.Fail(ErrorCodes.DuplicateEmail);
        }
        return await AuthService.SendCodeAsync(
            codes, mail, VerificationCodeService.EmailChangeKey(userId, email), email, cancellationToken);
    }

    public ServiceResult<EmptyData> VerifyEmailChangeCode(long userId, EmailCodeVerifyRequest request)
    {
        string email = InputRules.NormalizeEmail(request.Email);
        if (!InputRules.IsEmail(email) || !InputRules.IsCode(request.Code))
        {
            return ServiceResult<EmptyData>.Fail(ErrorCodes.InvalidInput);
        }
        return AuthService.CheckCode(codes, VerificationCodeService.EmailChangeKey(userId, email), request.Code!);
    }

    // 성공하면 바뀐 회원을 돌려준다. (세션의 이름 클레임 갱신용)
    public async Task<ServiceResult<User>> UpdateProfileAsync(long userId, ProfileRequest request)
    {
        string name = InputRules.Trim(request.Name);
        string email = InputRules.NormalizeEmail(request.Email);
        if (!InputRules.IsUserName(name) || !InputRules.IsEmail(email))
        {
            return ServiceResult<User>.Fail(ErrorCodes.InvalidInput);
        }
        User? user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.NotFound);
        }
        string key = VerificationCodeService.EmailChangeKey(userId, email);
        bool isEmailChanged = user.Email != email;
        if (isEmailChanged)
        {
            if (!codes.IsVerified(key))
            {
                return ServiceResult<User>.Fail(ErrorCodes.EmailNotVerified);
            }
            if (await db.Users.AnyAsync(u => u.Email == email && u.Id != userId))
            {
                return ServiceResult<User>.Fail(ErrorCodes.DuplicateEmail);
            }
        }
        user.Name = name;
        user.Email = email;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<User>.Fail(ErrorCodes.DuplicateEmail);
        }
        if (isEmailChanged)
        {
            codes.Consume(key);
        }
        return ServiceResult<User>.Ok(user);
    }

    public async Task<ServiceResult<EmptyData>> ChangePasswordAsync(long userId, PasswordChangeRequest request)
    {
        if (!InputRules.IsPasswordInput(request.CurrentPassword)
            || !InputRules.IsPassword(request.NewPassword)
            || request.NewPassword != request.NewPasswordConfirm)
        {
            return ServiceResult<EmptyData>.Fail(ErrorCodes.InvalidInput);
        }
        User? user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return ServiceResult<EmptyData>.Fail(ErrorCodes.NotFound);
        }
        if (!passwordHasher.Verify(request.CurrentPassword!, user.PasswordHash))
        {
            return ServiceResult<EmptyData>.Fail(ErrorCodes.WrongPassword);
        }
        user.PasswordHash = passwordHasher.Hash(request.NewPassword!);
        await db.SaveChangesAsync();
        return ServiceResult<EmptyData>.Ok(new EmptyData());
    }

    // 수신 내역 → 구독 → 회원 순서로 한 트랜잭션에서 지운다. 뉴스레터는 남긴다.
    public async Task<ServiceResult<RedirectResponse>> WithdrawAsync(long userId, WithdrawRequest request)
    {
        if (!InputRules.IsPasswordInput(request.Password))
        {
            return ServiceResult<RedirectResponse>.Fail(ErrorCodes.InvalidInput);
        }
        User? user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return ServiceResult<RedirectResponse>.Fail(ErrorCodes.NotFound);
        }
        if (user.IsAdmin)
        {
            return ServiceResult<RedirectResponse>.Fail(ErrorCodes.AdminCannotWithdraw);
        }
        if (!passwordHasher.Verify(request.Password!, user.PasswordHash))
        {
            return ServiceResult<RedirectResponse>.Fail(ErrorCodes.WrongPassword);
        }
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Historys.Where(h => h.UserId == userId).ExecuteDeleteAsync();
        await db.Subscriptions.Where(s => s.UserId == userId).ExecuteDeleteAsync();
        await db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync();
        await transaction.CommitAsync();
        return ServiceResult<RedirectResponse>.Ok(new RedirectResponse("/login"));
    }
}
