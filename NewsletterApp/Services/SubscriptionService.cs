using Microsoft.EntityFrameworkCore;
using NewsletterApp.Data;
using NewsletterApp.Data.Entities;
using NewsletterApp.Models;
using NewsletterApp.Models.Requests;
using NewsletterApp.Models.Responses;

namespace NewsletterApp.Services;

// 구독·해지 (내 구독, 구독 관리, 수신 거부 공통). 이미 원하는 상태면 성공으로 처리한다.
public sealed class SubscriptionService(AppDbContext db, IUnsubscribeTokenService tokens, IClock clock)
{
    public const int AdminPageSize = 10;

    public async Task<ServiceResult<ItemsResponse<MySubscriptionItem>>> GetMineAsync(long userId)
    {
        List<long> subscribed = await db.Subscriptions.Where(s => s.UserId == userId)
            .Select(s => s.GubunId).ToListAsync();
        var gubuns = await db.Gubuns.AsNoTracking().Include(g => g.Config).OrderBy(g => g.Name).ToListAsync();
        List<MySubscriptionItem> items = gubuns.Select(g => new MySubscriptionItem(
            g.Id,
            g.Name,
            g.Config is null ? null : InputRules.FormatSendTime(g.Config.SendTime),
            g.Config?.IsUse,
            subscribed.Contains(g.Id))).ToList();
        return ServiceResult<ItemsResponse<MySubscriptionItem>>.Ok(new ItemsResponse<MySubscriptionItem>(items));
    }

    public async Task<ServiceResult<SubscribedResponse>> SetMineAsync(long userId, MySubscriptionRequest request)
    {
        if (request.GubunId is not > 0 || request.IsSubscribed is null)
        {
            return ServiceResult<SubscribedResponse>.Fail(ErrorCodes.InvalidInput);
        }
        if (!await db.Gubuns.AnyAsync(g => g.Id == request.GubunId))
        {
            return ServiceResult<SubscribedResponse>.Fail(ErrorCodes.NotFound);
        }
        await SetAsync(userId, request.GubunId.Value, request.IsSubscribed.Value);
        return ServiceResult<SubscribedResponse>.Ok(new SubscribedResponse(request.IsSubscribed.Value));
    }

    // 정지 회원을 뺀 모든 회원. 이름 → 아이디 오름차순, 10명씩.
    public async Task<ServiceResult<PageResult<AdminSubscriptionItem>>> GetForAdminAsync(long? gubunId, int page)
    {
        if (gubunId is <= 0)
        {
            return ServiceResult<PageResult<AdminSubscriptionItem>>.Fail(ErrorCodes.InvalidInput);
        }
        if (gubunId is not null && !await db.Gubuns.AnyAsync(g => g.Id == gubunId))
        {
            return ServiceResult<PageResult<AdminSubscriptionItem>>.Fail(ErrorCodes.NotFound);
        }
        IQueryable<User> users = db.Users.AsNoTracking().Where(u => !u.IsSuspended);
        int totalCount = await users.CountAsync();
        if (!PageResult<AdminSubscriptionItem>.IsValidPage(page, totalCount, AdminPageSize))
        {
            return ServiceResult<PageResult<AdminSubscriptionItem>>.Fail(ErrorCodes.InvalidInput);
        }
        List<AdminSubscriptionItem> items = await users
            .OrderBy(u => u.Name).ThenBy(u => u.LoginId)
            .Skip((page - 1) * AdminPageSize).Take(AdminPageSize)
            .Select(u => new AdminSubscriptionItem(
                u.Id,
                u.Name,
                u.Email,
                gubunId == null
                    ? null
                    : db.Subscriptions.Any(s => s.UserId == u.Id && s.GubunId == gubunId)))
            .ToListAsync();
        return ServiceResult<PageResult<AdminSubscriptionItem>>.Ok(new PageResult<AdminSubscriptionItem>(
            items, page, AdminPageSize, totalCount, PageResult<AdminSubscriptionItem>.CountPages(totalCount, AdminPageSize)));
    }

    public async Task<ServiceResult<SubscribedResponse>> SetForAdminAsync(AdminSubscriptionRequest request)
    {
        if (request.UserId is not > 0 || request.GubunId is not > 0 || request.IsSubscribed is null)
        {
            return ServiceResult<SubscribedResponse>.Fail(ErrorCodes.InvalidInput);
        }
        bool isUserActive = await db.Users.AnyAsync(u => u.Id == request.UserId && !u.IsSuspended);
        bool isGubunFound = await db.Gubuns.AnyAsync(g => g.Id == request.GubunId);
        if (!isUserActive || !isGubunFound)
        {
            return ServiceResult<SubscribedResponse>.Fail(ErrorCodes.NotFound);
        }
        await SetAsync(request.UserId.Value, request.GubunId.Value, request.IsSubscribed.Value);
        return ServiceResult<SubscribedResponse>.Ok(new SubscribedResponse(request.IsSubscribed.Value));
    }

    // 수신 거부 화면을 열 때: 토큰이 올바르고 회원·종류가 있으면 종류 이름. 구독은 바꾸지 않는다.
    public async Task<string?> FindUnsubscribeGubunNameAsync(string? token)
    {
        if (!InputRules.IsToken(token) || tokens.Read(token!) is not { } target)
        {
            return null;
        }
        if (!await db.Users.AnyAsync(u => u.Id == target.UserId))
        {
            return null;
        }
        return await db.Gubuns.Where(g => g.Id == target.GubunId).Select(g => g.Name).SingleOrDefaultAsync();
    }

    public async Task<ServiceResult<UnsubscribeResponse>> UnsubscribeAsync(UnsubscribeRequest request)
    {
        if (!InputRules.IsToken(request.Token))
        {
            return ServiceResult<UnsubscribeResponse>.Fail(ErrorCodes.InvalidInput);
        }
        string? gubunName = await FindUnsubscribeGubunNameAsync(request.Token);
        UnsubscribeTarget? target = tokens.Read(request.Token!);
        if (gubunName is null || target is null)
        {
            return ServiceResult<UnsubscribeResponse>.Fail(ErrorCodes.InvalidLink);
        }
        await SetAsync(target.UserId, target.GubunId, false);
        return ServiceResult<UnsubscribeResponse>.Ok(new UnsubscribeResponse(gubunName));
    }

    private async Task SetAsync(long userId, long gubunId, bool isSubscribed)
    {
        if (!isSubscribed)
        {
            await db.Subscriptions.Where(s => s.UserId == userId && s.GubunId == gubunId).ExecuteDeleteAsync();
            return;
        }
        if (await db.Subscriptions.AnyAsync(s => s.UserId == userId && s.GubunId == gubunId))
        {
            return;
        }
        db.Subscriptions.Add(new Subscription { UserId = userId, GubunId = gubunId, CreateDate = clock.UtcNow });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // 동시에 같은 구독을 넣은 경우. 이미 구독 상태이므로 성공으로 본다.
            db.ChangeTracker.Clear();
        }
    }
}
