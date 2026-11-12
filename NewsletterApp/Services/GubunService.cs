using Microsoft.EntityFrameworkCore;
using NewsletterApp.Data;
using NewsletterApp.Data.Entities;
using NewsletterApp.Models;
using NewsletterApp.Models.Requests;
using NewsletterApp.Models.Responses;

namespace NewsletterApp.Services;

// 종류 목록·추가, 발송 설정 저장 (docs/api.md 6.7)
public sealed class GubunService(AppDbContext db)
{
    public async Task<ServiceResult<ItemsResponse<GubunItem>>> GetAllAsync()
    {
        var gubuns = await db.Gubuns.AsNoTracking().Include(g => g.Config).OrderBy(g => g.Name).ToListAsync();
        return ServiceResult<ItemsResponse<GubunItem>>.Ok(new ItemsResponse<GubunItem>(gubuns.Select(ToItem).ToList()));
    }

    public async Task<ServiceResult<GubunItem>> GetAsync(long id)
    {
        if (id <= 0)
        {
            return ServiceResult<GubunItem>.Fail(ErrorCodes.InvalidInput);
        }
        Gubun? gubun = await db.Gubuns.AsNoTracking().Include(g => g.Config).SingleOrDefaultAsync(g => g.Id == id);
        return gubun is null
            ? ServiceResult<GubunItem>.Fail(ErrorCodes.NotFound)
            : ServiceResult<GubunItem>.Ok(ToItem(gubun));
    }

    // 종류와 발송 설정을 한 번에 만든다.
    public async Task<ServiceResult<CreatedResponse>> CreateAsync(GubunCreateRequest request)
    {
        string name = InputRules.Trim(request.Name);
        if (!InputRules.IsGubunName(name)
            || !InputRules.TryParseSendTime(request.SendTime, out TimeOnly sendTime)
            || request.IsUse is null)
        {
            return ServiceResult<CreatedResponse>.Fail(ErrorCodes.InvalidInput);
        }
        if (await db.Gubuns.AnyAsync(g => g.Name == name))
        {
            return ServiceResult<CreatedResponse>.Fail(ErrorCodes.DuplicateGubunName);
        }
        var gubun = new Gubun
        {
            Name = name,
            Config = new NewsletterConfig { SendTime = sendTime, IsUse = request.IsUse.Value }
        };
        db.Gubuns.Add(gubun);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<CreatedResponse>.Fail(ErrorCodes.DuplicateGubunName);
        }
        return ServiceResult<CreatedResponse>.Ok(new CreatedResponse(gubun.Id));
    }

    // 발송 설정이 없으면 만든다.
    public async Task<ServiceResult<ConfigResponse>> SaveConfigAsync(long id, GubunConfigRequest request)
    {
        if (id <= 0 || !InputRules.TryParseSendTime(request.SendTime, out TimeOnly sendTime) || request.IsUse is null)
        {
            return ServiceResult<ConfigResponse>.Fail(ErrorCodes.InvalidInput);
        }
        Gubun? gubun = await db.Gubuns.Include(g => g.Config).SingleOrDefaultAsync(g => g.Id == id);
        if (gubun is null)
        {
            return ServiceResult<ConfigResponse>.Fail(ErrorCodes.NotFound);
        }
        gubun.Config ??= new NewsletterConfig();
        gubun.Config.SendTime = sendTime;
        gubun.Config.IsUse = request.IsUse.Value;
        await db.SaveChangesAsync();
        return ServiceResult<ConfigResponse>.Ok(
            new ConfigResponse(InputRules.FormatSendTime(sendTime), request.IsUse.Value));
    }

    private static GubunItem ToItem(Gubun gubun) => new(
        gubun.Id,
        gubun.Name,
        gubun.Config is null ? null : InputRules.FormatSendTime(gubun.Config.SendTime),
        gubun.Config?.IsUse);
}
