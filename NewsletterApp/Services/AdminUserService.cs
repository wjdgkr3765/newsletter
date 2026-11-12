using Microsoft.EntityFrameworkCore;
using NewsletterApp.Data;
using NewsletterApp.Models;
using NewsletterApp.Models.Requests;
using NewsletterApp.Models.Responses;

namespace NewsletterApp.Services;

// 회원 목록, 정지·정지 해제 (docs/api.md 6.5)
public sealed class AdminUserService(AppDbContext db)
{
    public const int PageSize = 10;

    public async Task<ServiceResult<PageResult<AdminUserItem>>> GetPageAsync(long currentUserId, int page)
    {
        int totalCount = await db.Users.CountAsync();
        if (!PageResult<AdminUserItem>.IsValidPage(page, totalCount, PageSize))
        {
            return ServiceResult<PageResult<AdminUserItem>>.Fail(ErrorCodes.InvalidInput);
        }
        List<AdminUserItem> items = await db.Users.AsNoTracking()
            .OrderBy(u => u.Name).ThenBy(u => u.LoginId)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(u => new AdminUserItem(
                u.Id, u.LoginId, u.Name, u.IsAdmin, u.IsSuspended, u.Id == currentUserId, u.CreateDate))
            .ToListAsync();
        return ServiceResult<PageResult<AdminUserItem>>.Ok(new PageResult<AdminUserItem>(
            items, page, PageSize, totalCount, PageResult<AdminUserItem>.CountPages(totalCount, PageSize)));
    }

    public async Task<ServiceResult<SuspensionResponse>> SetSuspensionAsync(
        long currentUserId, long targetUserId, SuspensionRequest request)
    {
        if (targetUserId <= 0 || request.IsSuspended is null)
        {
            return ServiceResult<SuspensionResponse>.Fail(ErrorCodes.InvalidInput);
        }
        if (request.IsSuspended.Value && targetUserId == currentUserId)
        {
            return ServiceResult<SuspensionResponse>.Fail(ErrorCodes.CannotSuspendSelf);
        }
        int count = await db.Users.Where(u => u.Id == targetUserId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.IsSuspended, request.IsSuspended.Value));
        if (count == 0)
        {
            return ServiceResult<SuspensionResponse>.Fail(ErrorCodes.NotFound);
        }
        return ServiceResult<SuspensionResponse>.Ok(new SuspensionResponse(request.IsSuspended.Value));
    }
}
