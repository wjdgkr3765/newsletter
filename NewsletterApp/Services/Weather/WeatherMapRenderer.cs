using System.Text.Json;
using SkiaSharp;

namespace NewsletterApp.Services.Weather;

// 전국 날씨 지도 PNG (docs/api.md 7번 "본문 만들기" 3). 배치는 승인된 시안 docs/mockups/weather_mail.html과 같다.
public sealed class WeatherMapRenderer(ILogger<WeatherMapRenderer> logger)
{
    public const int Size = 600;

    private const float Top = 50;
    private const float MinLon = 124.5f;
    private const float MaxLat = 38.7f;
    private const float PixelPerLon = 77.9f;
    private const float PixelPerLat = 96.2f;
    private const float LabelWidth = 104;
    private const float LabelHeight = 24;
    private const string FontFamily = "Malgun Gothic";

    private static readonly SKColor SeaColor = SKColor.Parse("#dbe9f6");
    private static readonly SKColor LandColor = SKColor.Parse("#f8f9fa");
    private static readonly SKColor LineColor = SKColor.Parse("#adb5bd");
    private static readonly SKColor TextColor = SKColor.Parse("#212529");
    private static readonly SKColor MutedColor = SKColor.Parse("#868e96");
    private static readonly SKColor BorderColor = SKColor.Parse("#ced4da");
    private static readonly SKColor HighlightColor = SKColor.Parse("#0d6efd");
    private static readonly SKColor SunColor = SKColor.Parse("#f5b301");
    private static readonly SKColor CloudColor = SKColor.Parse("#adb5bd");
    private static readonly SKColor RainColor = SKColor.Parse("#1c7ed6");
    private static readonly SKColor SnowColor = SKColor.Parse("#4dabf7");

    private readonly Lazy<List<SKPoint[]>?> _outline = new(() => LoadOutline(logger));

    // 실패하면 null (윤곽 파일이나 글꼴이 없을 때)
    public byte[]? Render(DateOnly day, string highlightName, IReadOnlyList<(WeatherRegion Region, DailyWeather? Weather)> regions)
    {
        List<SKPoint[]>? outline = _outline.Value;
        if (outline is null)
        {
            return null;
        }
        using SKTypeface? typeface = SKTypeface.FromFamilyName(FontFamily);
        if (typeface is null || typeface.FamilyName != FontFamily)
        {
            logger.LogError("날씨 지도 글꼴 없음: {Font}", FontFamily);
            return null;
        }
        try
        {
            using var surface = SKSurface.Create(new SKImageInfo(Size, Size));
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SeaColor);
            DrawLand(canvas, outline);
            using SKTypeface boldTypeface = SKTypeface.FromFamilyName(FontFamily, SKFontStyle.Bold);
            using var titleFont = new SKFont(boldTypeface, 20);
            using var labelFont = new SKFont(typeface, 12);
            using var boldLabelFont = new SKFont(boldTypeface, 12);
            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.DrawText($"오늘 날씨 {day:yyyy-MM-dd}", 16, 34, SKTextAlign.Left, titleFont, textPaint);
            foreach ((WeatherRegion region, DailyWeather? weather) in regions)
            {
                bool isHighlight = region.Name == highlightName;
                DrawRegion(canvas, region, weather, isHighlight, isHighlight ? boldLabelFont : labelFont, textPaint);
            }
            using SKImage image = surface.Snapshot();
            using SKData png = image.Encode(SKEncodedImageFormat.Png, 100);
            return png.ToArray();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "날씨 지도 그리기 실패");
            return null;
        }
    }

    internal static SKPoint ToPoint(double lon, double lat) =>
        new((float)((lon - MinLon) * PixelPerLon), (float)(Top + (MaxLat - lat) * PixelPerLat));

    private static void DrawLand(SKCanvas canvas, List<SKPoint[]> outline)
    {
        using var fill = new SKPaint { Color = LandColor, IsAntialias = true, Style = SKPaintStyle.Fill };
        using var stroke = new SKPaint { Color = LineColor, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
        foreach (SKPoint[] polygon in outline)
        {
            using var builder = new SKPathBuilder();
            builder.AddPoly(polygon, true);
            using SKPath path = builder.Detach();
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);
        }
    }

    private static void DrawRegion(
        SKCanvas canvas, WeatherRegion region, DailyWeather? weather, bool isHighlight, SKFont font, SKPaint textPaint)
    {
        SKPoint point = ToPoint(region.Lon, region.Lat);
        using var dot = new SKPaint { Color = SKColor.Parse("#495057"), IsAntialias = true };
        canvas.DrawCircle(point, 3, dot);

        var box = SKRect.Create(point.X + region.LabelDx, point.Y + region.LabelDy, LabelWidth, LabelHeight);
        using var boxFill = new SKPaint { Color = SKColors.White, IsAntialias = true };
        using var boxLine = new SKPaint
        {
            Color = isHighlight ? HighlightColor : BorderColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = isHighlight ? 2.5f : 1
        };
        canvas.DrawRoundRect(box, 4, 4, boxFill);
        canvas.DrawRoundRect(box, 4, 4, boxLine);

        // 지도 아이콘은 12시 날씨
        string? condition = weather?.Hours.FirstOrDefault(h => h.Time == new TimeOnly(12, 0))?.Condition;
        string text = weather is null
            ? $"{region.Name} -"
            : $"{region.Name} {weather.MinTemperature}/{weather.MaxTemperature}°";
        DrawIcon(canvas, condition, box.Left + 3, box.Top + 2, font);
        canvas.DrawText(text, box.Left + 27, box.Top + 16.5f, SKTextAlign.Left, font, textPaint);
    }

    // 아이콘은 코드로 그린다 (맑음, 구름많음, 흐림, 비, 소나기, 비/눈, 눈). 값이 없으면 "-".
    private static void DrawIcon(SKCanvas canvas, string? condition, float x, float y, SKFont font)
    {
        using var sun = new SKPaint { Color = SunColor, IsAntialias = true };
        using var cloud = new SKPaint { Color = CloudColor, IsAntialias = true };
        using var rain = new SKPaint { Color = RainColor, IsAntialias = true, StrokeWidth = 2 };
        using var snow = new SKPaint { Color = SnowColor, IsAntialias = true };
        switch (condition)
        {
            case "맑음":
                canvas.DrawCircle(x + 9, y + 9, 6, sun);
                break;
            case "구름많음":
                canvas.DrawCircle(x + 7, y + 7, 5, sun);
                DrawCloud(canvas, x, y, cloud);
                break;
            case "흐림":
                DrawCloud(canvas, x, y, cloud);
                break;
            case "비":
                DrawCloud(canvas, x, y, cloud);
                DrawRain(canvas, x, y, rain, 3, 5);
                break;
            case "소나기":
                canvas.DrawCircle(x + 7, y + 6, 4, sun);
                DrawCloud(canvas, x, y, cloud);
                DrawRain(canvas, x, y, rain, 3, 5);
                break;
            case "비/눈":
                DrawCloud(canvas, x, y, cloud);
                DrawRain(canvas, x, y, rain, 2, 8);
                canvas.DrawCircle(x + 10, y + 19, 1.6f, snow);
                break;
            case "눈":
                DrawCloud(canvas, x, y, cloud);
                for (int i = 0; i < 3; i++)
                {
                    canvas.DrawCircle(x + 6 + i * 5, y + 19, 1.6f, snow);
                }
                break;
            default:
                using (var muted = new SKPaint { Color = MutedColor, IsAntialias = true })
                {
                    canvas.DrawText("-", x + 6, y + 15, SKTextAlign.Left, font, muted);
                }
                break;
        }
    }

    private static void DrawCloud(SKCanvas canvas, float x, float y, SKPaint paint)
    {
        canvas.DrawOval(x + 11, y + 12, 9, 5, paint);
        canvas.DrawCircle(x + 8, y + 9, 5, paint);
        canvas.DrawCircle(x + 14, y + 8, 5, paint);
    }

    private static void DrawRain(SKCanvas canvas, float x, float y, SKPaint paint, int count, float gap)
    {
        for (int i = 0; i < count; i++)
        {
            canvas.DrawLine(x + 6 + i * gap, y + 17, x + 4 + i * gap, y + 21, paint);
        }
    }

    // Resources/korea_outline.json (Natural Earth, 경도·위도) → 지도 좌표
    private static List<SKPoint[]>? LoadOutline(ILogger logger)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Resources", "korea_outline.json");
        try
        {
            using FileStream stream = File.OpenRead(path);
            OutlineFile? file = JsonSerializer.Deserialize<OutlineFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (file?.Polygons is not { Count: > 0 } polygons)
            {
                logger.LogError("날씨 지도 윤곽 파일이 비어 있습니다.");
                return null;
            }
            return polygons
                .Select(polygon => polygon.Where(p => p.Length == 2).Select(p => ToPoint(p[0], p[1])).ToArray())
                .Where(points => points.Length >= 3)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            logger.LogError("날씨 지도 윤곽 파일을 읽지 못했습니다: {Type}", exception.GetType().Name);
            return null;
        }
    }

    private sealed record OutlineFile(List<List<double[]>>? Polygons);
}
