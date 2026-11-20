using Microsoft.EntityFrameworkCore;
using NewsletterApp.Data;
using NewsletterApp.Data.Entities;
using NewsletterApp.Models;
using NewsletterApp.Models.Requests;
using NewsletterApp.Models.Responses;

namespace NewsletterApp.Services;

// 뉴스레터 수신 내역 목록과 본문 (docs/api.md 6.3)
// 일반 사용자는 자기가 받은 것만, 관리자는 받은 회원이 1명 이상인 모든 뉴스레터.
public sealed class NewsletterQueryService(AppDbContext db)
{
    public const int PageSize = 30;

    public async Task<ServiceResult<NewsletterPageResponse>> GetPageAsync(
        long userId, bool isAdmin, int page, string? keyword = null)
    {
        if (!InputRules.TryNormalizeKeyword(keyword, out string search))
        {
            return ServiceResult<NewsletterPageResponse>.Fail(ErrorCodes.InvalidInput);
        }
        IQueryable<Newsletter> query = Visible(userId, isAdmin);
        if (search.Length > 0)
        {
            query = query.Where(n => n.Title.Contains(search) || n.Gubun!.Name.Contains(search));
        }
        int totalCount = await query.CountAsync();
        if (!PageResult<NewsletterListItem>.IsValidPage(page, totalCount, PageSize))
        {
            return ServiceResult<NewsletterPageResponse>.Fail(ErrorCodes.InvalidInput);
        }
        List<NewsletterListItem> items = await query
            .OrderByDescending(n => n.CreateDate).ThenByDescending(n => n.Id)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(n => new NewsletterListItem(
                n.Id,
                n.Title,
                n.Gubun!.Name,
                n.CreateDate,
                isAdmin ? n.Historys.Count : null))
            .ToListAsync();
        return ServiceResult<NewsletterPageResponse>.Ok(new NewsletterPageResponse(
            items, page, PageSize, totalCount, PageResult<NewsletterListItem>.CountPages(totalCount, PageSize), isAdmin));
    }

    public async Task<ServiceResult<NewsletterDetailResponse>> GetAsync(long userId, bool isAdmin, long id)
    {
        if (id <= 0)
        {
            return ServiceResult<NewsletterDetailResponse>.Fail(ErrorCodes.InvalidInput);
        }
        IQueryable<Newsletter> query = isAdmin
            ? db.Newsletters.AsNoTracking()
            : db.Newsletters.AsNoTracking().Where(n => n.Historys.Any(h => h.UserId == userId));
        NewsletterDetailResponse? detail = await query
            .Where(n => n.Id == id)
            .Select(n => new NewsletterDetailResponse(n.Id, n.Title, n.Gubun!.Name, n.CreateDate, n.Content))
            .SingleOrDefaultAsync();
        return detail is null
            ? ServiceResult<NewsletterDetailResponse>.Fail(ErrorCodes.NotFound)
            : ServiceResult<NewsletterDetailResponse>.Ok(detail);
    }

    private IQueryable<Newsletter> Visible(long userId, bool isAdmin) => isAdmin
        ? db.Newsletters.AsNoTracking().Where(n => n.Historys.Any())
        : db.Newsletters.AsNoTracking().Where(n => n.Historys.Any(h => h.UserId == userId));
}
