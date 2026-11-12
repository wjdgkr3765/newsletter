using NewsletterApp.Models.Requests;

namespace NewsletterApp.Tests.Validation;

// 입력값 규칙: 동등 분할과 경계값 (docs/api.md "입력값 규칙")
public sealed class InputRulesTests
{
    [Theory]
    [InlineData("abc12", false)]                   // 5자 (경계 아래)
    [InlineData("abc123", true)]                   // 6자 (경계)
    [InlineData("abcdefghij1234567890", true)]     // 20자 (경계)
    [InlineData("abcdefghij12345678901", false)]   // 21자 (경계 위)
    [InlineData("Abc123", false)]                  // 대문자
    [InlineData("abc_123", false)]                 // 특수문자
    [InlineData("", false)]
    public void IsLoginId(string value, bool expected) => Assert.Equal(expected, InputRules.IsLoginId(value));

    [Theory]
    [InlineData("abcd12!", false)]                 // 7자 (경계 아래)
    [InlineData("abcde12!", true)]                 // 8자 (경계)
    [InlineData("abcdefghijklmnop12!@", true)]     // 20자 (경계)
    [InlineData("abcdefghijklmnop12!@x", false)]   // 21자 (경계 위)
    [InlineData("abcdefgh1", false)]               // 특수문자 없음
    [InlineData("abcdefgh!", false)]               // 숫자 없음
    [InlineData("12345678!", false)]               // 영문 없음
    [InlineData("abc de12!", false)]               // 공백
    [InlineData("한글abcd12!", false)]              // ASCII 아님
    [InlineData(null, false)]
    public void IsPassword(string? value, bool expected) => Assert.Equal(expected, InputRules.IsPassword(value));

    [Theory]
    [InlineData("a@b.co", true)]
    [InlineData("ab", false)]
    [InlineData("a@b", false)]
    [InlineData("a b@c.com", false)]
    public void IsEmail_Format(string value, bool expected) => Assert.Equal(expected, InputRules.IsEmail(value));

    [Fact]
    public void IsEmail_Length()
    {
        string fifty = new string('a', 44) + "@b.com";       // 50자 (경계)
        string fiftyOne = new string('a', 45) + "@b.com";    // 51자 (경계 위)
        Assert.Equal(50, fifty.Length);
        Assert.True(InputRules.IsEmail(fifty));
        Assert.False(InputRules.IsEmail(fiftyOne));
    }

    [Fact]
    public void NormalizeEmail_TrimsAndLowercases() =>
        Assert.Equal("hong@example.com", InputRules.NormalizeEmail("  Hong@Example.COM "));

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void IsUserName(int length, bool expected) =>
        Assert.Equal(expected, InputRules.IsUserName(new string('가', length)));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void IsGubunName(int length, bool expected) =>
        Assert.Equal(expected, InputRules.IsGubunName(new string('가', length)));

    [Theory]
    [InlineData("12345", false)]
    [InlineData("123456", true)]
    [InlineData("1234567", false)]
    [InlineData("12345a", false)]
    [InlineData(null, false)]
    public void IsCode(string? value, bool expected) => Assert.Equal(expected, InputRules.IsCode(value));

    [Theory]
    [InlineData("00:00", true)]
    [InlineData("23:59", true)]
    [InlineData("24:00", false)]
    [InlineData("12:60", false)]
    [InlineData("9:00", false)]
    [InlineData(null, false)]
    public void TryParseSendTime(string? value, bool expected) =>
        Assert.Equal(expected, InputRules.TryParseSendTime(value, out _));

    [Fact]
    public void TryParseSendTime_Value()
    {
        Assert.True(InputRules.TryParseSendTime("16:05", out TimeOnly time));
        Assert.Equal(new TimeOnly(16, 5), time);
        Assert.Equal("16:05", InputRules.FormatSendTime(time));
    }
}
