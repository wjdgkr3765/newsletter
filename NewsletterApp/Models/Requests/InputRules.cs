using System.Text.RegularExpressions;

namespace NewsletterApp.Models.Requests;

// docs/api.md "입력값 규칙". 서버에서 모든 입력값을 이 규칙으로 검증한다.
public static partial class InputRules
{
    public const int MaxPasswordLength = 20;
    public const int MaxEmailLength = 50;
    public const int MaxTokenLength = 1000;
    public const int MaxKeywordLength = 50;

    [GeneratedRegex("^[a-z0-9]{6,20}$")]
    private static partial Regex LoginIdRegex();

    [GeneratedRegex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex("^[0-9]{6}$")]
    private static partial Regex CodeRegex();

    [GeneratedRegex("^([01][0-9]|2[0-3]):[0-5][0-9]$")]
    private static partial Regex TimeRegex();

    public static string Trim(string? value) => (value ?? "").Trim();

    public static string NormalizeEmail(string? value) => Trim(value).ToLowerInvariant();

    public static bool IsLoginId(string value) => LoginIdRegex().IsMatch(value);

    // 8~20자, 영문·숫자·특수문자(영문·숫자·공백이 아닌 출력 가능한 ASCII) 각 1자 이상
    public static bool IsPassword(string? value)
    {
        if (value is null || value.Length < 8 || value.Length > MaxPasswordLength)
        {
            return false;
        }
        bool hasLetter = false;
        bool hasDigit = false;
        bool hasSpecial = false;
        foreach (char c in value)
        {
            if (char.IsAsciiLetter(c))
            {
                hasLetter = true;
            }
            else if (char.IsAsciiDigit(c))
            {
                hasDigit = true;
            }
            else if (c > ' ' && c < 127)
            {
                hasSpecial = true;
            }
            else
            {
                return false;
            }
        }
        return hasLetter && hasDigit && hasSpecial;
    }

    // 로그인·현재 비밀번호 확인용: 1~20자
    public static bool IsPasswordInput(string? value) =>
        !string.IsNullOrEmpty(value) && value.Length <= MaxPasswordLength;

    public static bool IsEmail(string value) => value.Length <= MaxEmailLength && EmailRegex().IsMatch(value);

    public static bool IsUserName(string value) => value.Length is >= 2 and <= 30;

    public static bool IsGubunName(string value) => value.Length is >= 1 and <= 20;

    public static bool IsCode(string? value) => value is not null && CodeRegex().IsMatch(value);

    public static bool IsToken(string? value) => !string.IsNullOrEmpty(value) && value.Length <= MaxTokenLength;

    // 목록 검색어: 앞뒤 공백을 뺀 뒤 0~50자. 빈 값이면 검색하지 않는다. (D-012)
    public static bool TryNormalizeKeyword(string? value, out string keyword)
    {
        keyword = Trim(value);
        return keyword.Length <= MaxKeywordLength;
    }

    public static bool TryParseSendTime(string? value, out TimeOnly time)
    {
        time = default;
        if (value is null || !TimeRegex().IsMatch(value))
        {
            return false;
        }
        time = new TimeOnly(int.Parse(value[..2]), int.Parse(value[3..]));
        return true;
    }

    public static string FormatSendTime(TimeOnly time) => time.ToString("HH:mm");
}
