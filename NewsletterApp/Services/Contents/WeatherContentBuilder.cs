using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using NewsletterApp.Services.Weather;

namespace NewsletterApp.Services.Contents;

// "날씨 정보({지역})" 본문 생성기 (docs/api.md 7번 "본문 만들기"). 지역마다 하나씩 등록한다.
public sealed class WeatherContentBuilder(
    WeatherRegion region,
    IReadOnlyList<WeatherRegion> allRegions,
    IWeatherForecastClient forecastClient,
    WeatherMapRenderer mapRenderer) : INewsletterContentBuilder
{
    public const string Attribution =
        "이 메일의 날씨 정보는 기상청이 공공누리 제1유형으로 개방한 '기상청_단기예보 조회서비스'를 이용했으며, "
        + "공공데이터포털(www.data.go.kr)에서 무료로 받을 수 있습니다.";

    private const string Cell = "border: 1px solid #dee2e6; padding: 6px 10px";

    public string GubunName { get; } = GetGubunName(region);

    public static string GetGubunName(WeatherRegion region) => $"날씨 정보({region.Name})";

    public async Task<string?> BuildAsync(string gubunName, DateOnly kstDay, CancellationToken cancellationToken)
    {
        DailyWeather? own = await forecastClient.GetTodayAsync(region, cancellationToken);
        if (own is null)
        {
            return null;
        }
        // 다른 지역은 받지 못해도 지도에 "-"로 그린다.
        DailyWeather?[] weathers = await Task.WhenAll(allRegions.Select(r =>
            r == region ? Task.FromResult<DailyWeather?>(own) : forecastClient.GetTodayAsync(r, cancellationToken)));
        var mapRegions = allRegions.Select((r, i) => (r, weathers[i])).ToList();
        byte[]? map = mapRenderer.Render(kstDay, region.Name, mapRegions);
        if (map is null)
        {
            return null;
        }
        return BuildHtml(kstDay, own, map);
    }

    private string BuildHtml(DateOnly kstDay, DailyWeather weather, byte[] map)
    {
        // 한글은 그대로 두고 <, >, &, 따옴표만 바꾼다. (HtmlEncoder.Default는 한글도 &#x..;로 바꾼다)
        HtmlEncoder encoder = HtmlEncoder.Create(UnicodeRanges.All);
        string day = kstDay.ToString("yyyy-MM-dd");
        string name = encoder.Encode(region.Name);
        var html = new StringBuilder();
        html.Append("<div style=\"font-family: sans-serif; color: #212529; font-size: 15px; line-height: 1.5\">");
        html.Append("<div style=\"margin: 0 0 16px\"><img src=\"data:image/png;base64,")
            .Append(Convert.ToBase64String(map))
            .Append("\" alt=\"오늘 전국 날씨 지도\" width=\"600\" style=\"max-width: 100%; height: auto; display: block\"></div>");
        html.Append($"<h2 style=\"font-size: 20px; margin: 0 0 8px\">오늘({day}) {name} 날씨</h2>");
        html.Append($"<p style=\"margin: 0 0 16px\">최저 {weather.MinTemperature}°C / 최고 {weather.MaxTemperature}°C</p>");
        html.Append("<table style=\"border-collapse: collapse; width: 100%; max-width: 480px\"><thead><tr style=\"background: #f1f3f5\">");
        html.Append($"<th style=\"{Cell}; text-align: left\">시간</th><th style=\"{Cell}; text-align: left\">날씨</th>");
        html.Append($"<th style=\"{Cell}; text-align: right\">기온</th><th style=\"{Cell}; text-align: right\">강수 확률</th>");
        html.Append("</tr></thead><tbody>");
        foreach (HourlyWeather hour in weather.Hours)
        {
            html.Append($"<tr><td style=\"{Cell}\">{hour.Time:HH\\:mm}</td><td style=\"{Cell}\">{encoder.Encode(hour.Condition)}</td>");
            html.Append($"<td style=\"{Cell}; text-align: right\">{hour.Temperature}°C</td>");
            html.Append($"<td style=\"{Cell}; text-align: right\">{hour.RainChance}%</td></tr>");
        }
        html.Append("</tbody></table>");
        html.Append($"<p style=\"color: #6c757d; font-size: 12px; margin: 16px 0 0\">{encoder.Encode(Attribution)}</p>");
        html.Append("</div>");
        return html.ToString();
    }
}
