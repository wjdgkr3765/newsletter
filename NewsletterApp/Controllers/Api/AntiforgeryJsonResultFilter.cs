using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using NewsletterApp.Models;

namespace NewsletterApp.Controllers.Api;

// CSRF 토큰이 없거나 틀린 POST를 공통 응답 형식(400 INVALID_INPUT)으로 바꾼다.
public sealed class AntiforgeryJsonResultFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is IAntiforgeryValidationFailedResult)
        {
            context.Result = new ObjectResult(ApiControllerBase.CreateError(ErrorCodes.InvalidInput))
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
