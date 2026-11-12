namespace NewsletterApp.Models;

// docs/api.md "3. 공통 응답 형식"
public sealed record ApiResponse<T>(T Data);

public sealed record ApiErrorResponse(ApiError Error);

public sealed record ApiError(string Code, string Message);

// 응답 data가 없을 때 쓰는 빈 객체 ({})
public sealed record EmptyData;

// docs/api.md "목록 응답"
public sealed record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages)
{
    public static int CountPages(int totalCount, int pageSize) => (totalCount + pageSize - 1) / pageSize;

    // page는 1 이상, 데이터가 있으면 마지막 페이지 이하
    public static bool IsValidPage(int page, int totalCount, int pageSize) =>
        page >= 1 && page <= Math.Max(1, CountPages(totalCount, pageSize));
}

// 서비스 처리 결과. 실패면 ErrorCode가 있다.
public sealed class ServiceResult<T>
{
    private ServiceResult(T? data, string? errorCode, string? message)
    {
        Data = data;
        ErrorCode = errorCode;
        Message = message;
    }

    public T? Data { get; }

    public string? ErrorCode { get; }

    // 기본 메시지 대신 보여줄 메시지 (예: 남은 횟수)
    public string? Message { get; }

    public bool IsSuccess => ErrorCode is null;

    public static ServiceResult<T> Ok(T data) => new(data, null, null);

    public static ServiceResult<T> Fail(string errorCode, string? message = null) => new(default, errorCode, message);
}
