namespace NewsletterApp.Models.Responses;

// API 응답 data. 필드는 docs/api.md "6. 실제 사용할 API"와 같다.

public sealed record ItemsResponse<T>(IReadOnlyList<T> Items);

public sealed record ExpiresResponse(int ExpiresInSeconds);

public sealed record AvailabilityResponse(bool Available);

public sealed record RedirectResponse(string RedirectUrl);

public sealed record MeResponse(string LoginId, string Name, string Email, bool IsAdmin, DateTime CreateDate);

public sealed record ProfileResponse(string Name, string Email);

public sealed record MySubscriptionItem(long GubunId, string Name, string? SendTime, bool? IsUse, bool IsSubscribed);

public sealed record SubscribedResponse(bool IsSubscribed);

public sealed record NewsletterListItem(long Id, string Title, string GubunName, DateTime SendDate, int? SubscriberCount);

public sealed record NewsletterPageResponse(
    IReadOnlyList<NewsletterListItem> Items, int Page, int PageSize, int TotalCount, int TotalPages, bool IsAdmin);

public sealed record NewsletterDetailResponse(long Id, string Title, string GubunName, DateTime SendDate, string Content);

public sealed record UnsubscribeResponse(string GubunName);

public sealed record AdminUserItem(
    long Id, string LoginId, string Name, bool IsAdmin, bool IsSuspended, bool IsMe, DateTime CreateDate);

public sealed record SuspensionResponse(bool IsSuspended);

public sealed record AdminSubscriptionItem(long UserId, string Name, string Email, bool? IsSubscribed);

public sealed record GubunItem(long Id, string Name, string? SendTime, bool? IsUse);

public sealed record CreatedResponse(long Id);

public sealed record ConfigResponse(string SendTime, bool IsUse);

public sealed record TodayResponse(
    bool Exists,
    string SendDay,
    long? NewsletterId,
    string? Title,
    DateTime? SendDate,
    int ReceivedCount,
    int PendingCount,
    bool IsSending);

public sealed record SendResponse(long NewsletterId, int ReceivedCount, int FailedCount);

public sealed record ResendResponse(int ReceivedCount, int FailedCount);
