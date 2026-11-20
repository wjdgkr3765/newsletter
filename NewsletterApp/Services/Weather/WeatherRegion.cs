namespace NewsletterApp.Services.Weather;

// 날씨 지역 설정 (appsettings.json Weather:Regions)
public sealed class WeatherRegion
{
    // 종류 이름 "날씨 정보({Name})"의 괄호 안 지역 이름
    public string Name { get; set; } = "";

    // 기상청 격자 좌표
    public int Nx { get; set; }

    public int Ny { get; set; }

    // 지도에 점을 찍을 위치
    public double Lat { get; set; }

    public double Lon { get; set; }

    // 이름표 왼쪽 위 모서리를 점에서 옮길 px
    public int LabelDx { get; set; }

    public int LabelDy { get; set; }
}

// 한 시각의 예보. Condition은 맑음, 구름많음, 흐림, 비, 비/눈, 눈, 소나기 중 하나.
public sealed record HourlyWeather(TimeOnly Time, string Condition, int Temperature, int RainChance);

// 한 지역의 오늘 예보 (최저·최고 기온, 06~21시 3시간 간격)
public sealed record DailyWeather(int MinTemperature, int MaxTemperature, IReadOnlyList<HourlyWeather> Hours);
