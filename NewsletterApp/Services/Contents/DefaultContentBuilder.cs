using System.Text.Encodings.Web;

namespace NewsletterApp.Services.Contents;

// 기본 본문: 종류 이름과 날짜만 들어간다.
public sealed class DefaultContentBuilder
{
    public string Build(string gubunName, DateOnly kstDay)
    {
        string name = HtmlEncoder.Default.Encode(gubunName);
        string day = kstDay.ToString("yyyy-MM-dd");
        return $"<h2>[{name}] {day}</h2><p>{name} 뉴스레터입니다.</p>";
    }
}
