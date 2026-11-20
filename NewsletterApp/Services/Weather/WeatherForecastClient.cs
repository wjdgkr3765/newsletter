using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace NewsletterApp.Services.Weather;

public interface IWeatherForecastClient
{
    // 그 지역의 오늘 예보. 받지 못하면 null.
    Task<DailyWeather?> GetTodayAsync(WeatherRegion region, CancellationToken cancellationToken);
}

// 기상청 단기예보 조회 (docs/api.md 7번 "예보 받기"). 인증키는 .env(Weather__ServiceKey)에서 읽는다.
public sealed class WeatherForecastClient(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IClock clock,
    IConfiguration configuration,
    ILogger<WeatherForecastClient> logger) : IWeatherForecastClient
{
    public const string HttpClientName = "Weather";

    // 표에 쓰는 시각
    public static readonly TimeOnly[] TableTimes =
        [new(6, 0), new(9, 0), new(12, 0), new(15, 0), new(18, 0), new(21, 0)];

    // 02시 발표를 API로 받을 수 있는 시각 (발표 10분 뒤)
    private static readonly TimeOnly TodayBaseAvailable = new(2, 10);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Dictionary<string, string> SkyNames = new()
    {
        ["1"] = "맑음",
        ["3"] = "구름많음",
        ["4"] = "흐림"
    };

    private static readonly Dictionary<string, string> RainNames = new()
    {
        ["1"] = "비",
        ["2"] = "비/눈",
        ["3"] = "눈",
        ["4"] = "소나기"
    };

    public async Task<DailyWeather?> GetTodayAsync(WeatherRegion region, CancellationToken cancellationToken)
    {
        DateTime kstNow = clock.KstNow;
        DateOnly today = DateOnly.FromDateTime(kstNow);
        (DateOnly baseDate, string baseTime) = ChooseBase(kstNow);
        string cacheKey = $"weather:{baseDate:yyyyMMdd}{baseTime}:{region.Nx},{region.Ny}";
        if (cache.TryGetValue(cacheKey, out DailyWeather? cached) && cached is not null)
        {
            return cached;
        }

        string? body = await RequestAsync(region, baseDate, baseTime, cancellationToken);
        if (body is null)
        {
            return null;
        }
        DailyWeather? weather = Parse(body, today, out string failReason);
        if (weather is null)
        {
            logger.LogWarning("날씨 예보 실패 {Region}: {Reason}", region.Name, failReason);
            return null;
        }
        cache.Set(cacheKey, weather, TimeToDayEnd(kstNow));
        return weather;
    }

    // 그날 끝(한국 시간 다음 날 00:00)까지 남은 시간. 지역마다 하루 한 번만 부른다. (D-011)
    internal static TimeSpan TimeToDayEnd(DateTime kstNow) => kstNow.Date.AddDays(1) - kstNow;

    // 02:10(한국 시간) 이후면 오늘 02시 발표, 그 전이면 어제 23시 발표
    internal static (DateOnly BaseDate, string BaseTime) ChooseBase(DateTime kstNow)
    {
        DateOnly today = DateOnly.FromDateTime(kstNow);
        return TimeOnly.FromDateTime(kstNow) >= TodayBaseAvailable
            ? (today, "0200")
            : (today.AddDays(-1), "2300");
    }

    private async Task<string?> RequestAsync(
        WeatherRegion region, DateOnly baseDate, string baseTime, CancellationToken cancellationToken)
    {
        string baseUrl = configuration["Weather:BaseUrl"] ?? "";
        string serviceKey = configuration["Weather:ServiceKey"] ?? "";
        // 인증키가 들어간 주소는 로그에 남기지 않는다. (security.md 1번)
        string url = $"{baseUrl}?serviceKey={Uri.EscapeDataString(serviceKey)}&pageNo=1&numOfRows=1000&dataType=JSON"
            + $"&base_date={baseDate:yyyyMMdd}&base_time={baseTime}&nx={region.Nx}&ny={region.Ny}";
        try
        {
            HttpClient client = httpClientFactory.CreateClient(HttpClientName);
            using HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("날씨 예보 실패 {Region}: HTTP {Status}", region.Name, (int)response.StatusCode);
                return null;
            }
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested
            && exception is HttpRequestException or TaskCanceledException)
        {
            // TaskCanceledException은 10초 시간 초과
            logger.LogWarning("날씨 예보 실패 {Region}: {Type}", region.Name, exception.GetType().Name);
            return null;
        }
    }

    // 응답에서 오늘 값만 꺼낸다. 하나라도 없거나 읽을 수 없으면 null.
    internal static DailyWeather? Parse(string body, DateOnly today, out string failReason)
    {
        KmaResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<KmaResponse>(body, JsonOptions);
        }
        catch (JsonException)
        {
            // 인증키 오류 등은 JSON이 아닌 XML로 올 수 있다.
            failReason = "응답 형식 오류";
            return null;
        }
        string? resultCode = parsed?.Response?.Header?.ResultCode;
        if (resultCode != "00")
        {
            failReason = $"resultCode {resultCode ?? "없음"}";
            return null;
        }

        string todayText = today.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var values = new Dictionary<(string Category, string Time), string>();
        string? min = null;
        string? max = null;
        foreach (KmaItem item in parsed!.Response!.Body?.Items?.Item ?? [])
        {
            if (item.FcstDate != todayText || item.Category is null || item.FcstTime is null || item.FcstValue is null)
            {
                continue;
            }
            if (item.Category == "TMN")
            {
                min = item.FcstValue;
            }
            else if (item.Category == "TMX")
            {
                max = item.FcstValue;
            }
            else
            {
                values[(item.Category, item.FcstTime)] = item.FcstValue;
            }
        }

        if (!TryReadNumber(min, out int minTemperature) || !TryReadNumber(max, out int maxTemperature))
        {
            failReason = "최저·최고 기온 없음";
            return null;
        }
        var hours = new List<HourlyWeather>();
        foreach (TimeOnly time in TableTimes)
        {
            string key = time.ToString("HHmm", CultureInfo.InvariantCulture);
            if (!values.TryGetValue(("TMP", key), out string? temperature) || !TryReadNumber(temperature, out int temperatureValue)
                || !values.TryGetValue(("POP", key), out string? rainChance) || !TryReadNumber(rainChance, out int rainChanceValue)
                || !values.TryGetValue(("SKY", key), out string? sky) || !values.TryGetValue(("PTY", key), out string? rain))
            {
                failReason = $"{key} 값 없음";
                return null;
            }
            string? condition = rain == "0" ? SkyNames.GetValueOrDefault(sky) : RainNames.GetValueOrDefault(rain);
            if (condition is null)
            {
                failReason = $"{key} 코드값 오류 SKY {sky}, PTY {rain}";
                return null;
            }
            hours.Add(new HourlyWeather(time, condition, temperatureValue, rainChanceValue));
        }
        failReason = "";
        return new DailyWeather(minTemperature, maxTemperature, hours);
    }

    // "2", "-1.0", "11.4" 같은 값을 반올림한 정수로 읽는다. 기온·강수 확률은 int 범위 안이다.
    private static bool TryReadNumber(string? text, out int value)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
            && number is > -100 and < 1000)
        {
            value = (int)Math.Round(number, MidpointRounding.AwayFromZero);
            return true;
        }
        value = 0;
        return false;
    }

    private sealed record KmaResponse(KmaRoot? Response);

    private sealed record KmaRoot(KmaHeader? Header, KmaBody? Body);

    private sealed record KmaHeader(string? ResultCode);

    private sealed record KmaBody(KmaItems? Items);

    private sealed record KmaItems(List<KmaItem>? Item);

    private sealed record KmaItem(string? FcstDate, string? FcstTime, string? Category, string? FcstValue);
}
