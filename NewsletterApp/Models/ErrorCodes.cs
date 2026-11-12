namespace NewsletterApp.Models;

// docs/api.md "4. 오류 코드"
public static class ErrorCodes
{
    public const string InvalidInput = "INVALID_INPUT";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string InternalError = "INTERNAL_ERROR";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string LoginLocked = "LOGIN_LOCKED";
    public const string DuplicateLoginId = "DUPLICATE_LOGIN_ID";
    public const string DuplicateEmail = "DUPLICATE_EMAIL";
    public const string SameEmail = "SAME_EMAIL";
    public const string CodeInvalid = "CODE_INVALID";
    public const string CodeExpired = "CODE_EXPIRED";
    public const string EmailNotVerified = "EMAIL_NOT_VERIFIED";
    public const string MemberNotMatched = "MEMBER_NOT_MATCHED";
    public const string WrongPassword = "WRONG_PASSWORD";
    public const string AdminCannotWithdraw = "ADMIN_CANNOT_WITHDRAW";
    public const string CannotSuspendSelf = "CANNOT_SUSPEND_SELF";
    public const string DuplicateGubunName = "DUPLICATE_GUBUN_NAME";
    public const string NewsletterExists = "NEWSLETTER_EXISTS";
    public const string NoSubscriber = "NO_SUBSCRIBER";
    public const string NoPendingSubscriber = "NO_PENDING_SUBSCRIBER";
    public const string SendingInProgress = "SENDING_IN_PROGRESS";
    public const string ContentFetchFailed = "CONTENT_FETCH_FAILED";
    public const string SendFailed = "SEND_FAILED";
    public const string MailSendFailed = "MAIL_SEND_FAILED";
    public const string InvalidLink = "INVALID_LINK";

    private static readonly Dictionary<string, (int Status, string Message)> Definitions = new()
    {
        [InvalidInput] = (400, "입력값이 올바르지 않습니다."),
        [Unauthorized] = (401, "로그인이 필요합니다."),
        [Forbidden] = (403, "권한이 없습니다."),
        [NotFound] = (404, "대상이 없습니다."),
        [InternalError] = (500, "일시적인 오류가 발생했습니다. 잠시 뒤에 다시 시도해 주세요."),
        [LoginFailed] = (400, "아이디 또는 비밀번호가 맞지 않습니다."),
        [LoginLocked] = (429, "로그인 시도가 많아 10분 뒤에 다시 시도해 주세요."),
        [DuplicateLoginId] = (409, "이미 사용 중인 아이디입니다."),
        [DuplicateEmail] = (409, "이미 사용 중인 이메일입니다."),
        [SameEmail] = (400, "지금 쓰고 있는 이메일입니다."),
        [CodeInvalid] = (400, "인증 코드가 맞지 않습니다."),
        [CodeExpired] = (400, "인증 코드가 만료되었거나 더 이상 쓸 수 없습니다. 다시 받아 주세요."),
        [EmailNotVerified] = (400, "이메일 인증을 먼저 완료해 주세요."),
        [MemberNotMatched] = (400, "아이디와 이메일이 일치하는 회원이 없습니다."),
        [WrongPassword] = (400, "현재 비밀번호가 맞지 않습니다."),
        [AdminCannotWithdraw] = (403, "관리자는 탈퇴할 수 없습니다."),
        [CannotSuspendSelf] = (400, "자기 자신은 정지할 수 없습니다."),
        [DuplicateGubunName] = (409, "이미 있는 종류 이름입니다."),
        [NewsletterExists] = (409, "오늘 뉴스레터가 이미 있습니다."),
        [NoSubscriber] = (400, "구독한 회원이 없어 만들지 않았습니다."),
        [NoPendingSubscriber] = (400, "받지 않은 구독자가 없습니다."),
        [SendingInProgress] = (409, "이 종류의 뉴스레터를 보내는 중입니다. 잠시 뒤에 다시 시도해 주세요."),
        [ContentFetchFailed] = (502, "본문 데이터를 받지 못해 만들지 않았습니다."),
        [SendFailed] = (502, "아무에게도 보내지 못했습니다."),
        [MailSendFailed] = (502, "메일을 보내지 못했습니다. 잠시 뒤에 다시 시도해 주세요."),
        [InvalidLink] = (400, "링크가 올바르지 않거나 더 이상 쓸 수 없습니다.")
    };

    public static int GetStatus(string code) => Definitions[code].Status;

    public static string GetMessage(string code) => Definitions[code].Message;
}
