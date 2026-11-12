namespace NewsletterApp.Models.Requests;

// API 요청 본문. 값이 빠질 수 있으므로 모두 nullable로 받고 InputRules로 검증한다.

public sealed record EmailRequest(string? Email);

public sealed record EmailCodeVerifyRequest(string? Email, string? Code);

public sealed record RegisterRequest(string? LoginId, string? Password, string? PasswordConfirm, string? Email, string? Name);

public sealed record LoginRequest(string? LoginId, string? Password);

public sealed record PasswordResetCodeRequest(string? LoginId, string? Email);

public sealed record PasswordResetVerifyRequest(string? LoginId, string? Email, string? Code);

public sealed record PasswordResetRequest(string? LoginId, string? Email, string? NewPassword, string? NewPasswordConfirm);

public sealed record ProfileRequest(string? Name, string? Email);

public sealed record PasswordChangeRequest(string? CurrentPassword, string? NewPassword, string? NewPasswordConfirm);

public sealed record WithdrawRequest(string? Password);

public sealed record MySubscriptionRequest(long? GubunId, bool? IsSubscribed);

public sealed record UnsubscribeRequest(string? Token);

public sealed record SuspensionRequest(bool? IsSuspended);

public sealed record AdminSubscriptionRequest(long? UserId, long? GubunId, bool? IsSubscribed);

public sealed record GubunCreateRequest(string? Name, string? SendTime, bool? IsUse);

public sealed record GubunConfigRequest(string? SendTime, bool? IsUse);
