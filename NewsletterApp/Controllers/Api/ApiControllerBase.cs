using Microsoft.AspNetCore.Mvc;
using NewsletterApp.Models;

namespace NewsletterApp.Controllers.Api;

// API 컨트롤러 공통: 서비스 결과를 docs/api.md 공통 응답 형식으로 바꾼다.
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected long CurrentUserId => User.GetUserId();

    protected IActionResult ToResult<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(new ApiResponse<T>(result.Data!));
        }
        return Error(result.ErrorCode!, result.Message);
    }

    protected IActionResult Error(string code, string? message = null) =>
        StatusCode(ErrorCodes.GetStatus(code),
            new ApiErrorResponse(new ApiError(code, message ?? ErrorCodes.GetMessage(code))));

    public static ApiErrorResponse CreateError(string code) =>
        new(new ApiError(code, ErrorCodes.GetMessage(code)));
}
