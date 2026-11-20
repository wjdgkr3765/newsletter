using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using NewsletterApp.Services;
using NewsletterApp.Services.Contents;
using NewsletterApp.Services.Weather;

namespace NewsletterApp.Tests.Services;

// 날씨 정보: 예보 받기, 값 꺼내기, 지도 그리기, 본문 만들기, 메일 이미지
public sealed class WeatherContentBuilderTests
{
    // FakeClock 기본 시각 2026-11-12 07:00 UTC = 한국 16:00
    private static readonly DateOnly Today = new(2026, 11, 12);

    private static readonly WeatherRegion Seoul = new() { Name = "서울", Nx = 60, Ny = 127, Lat = 37.56, Lon = 126.98, LabelDx = 8, LabelDy = -30 };

    private static readonly WeatherRegion Busan = new() { Name = "부산", Nx = 98, Ny = 76, Lat = 35.18, Lon = 129.08, LabelDx = 8, LabelDy = 2 };

    // ---------- 발표 고르기 (경계값: 02:10) ----------

    [Theory]
    [InlineData(0, 0, "2026-11-11", "2300")]
    [InlineData(2, 9, "2026-11-11", "2300")]
    [InlineData(2, 10, "2026-11-12", "0200")]
    [InlineData(2, 11, "2026-11-12", "0200")]
    [InlineData(23, 59, "2026-11-12", "0200")]
    public void ChooseBase_ByKstTime(int hour, int minute, string baseDate, string baseTime)
    {
        (DateOnly date, string time) = WeatherForecastClient.ChooseBase(new DateTime(2026, 11, 12, hour, minute, 0));
        Assert.Equal(DateOnly.Parse(baseDate), date);
        Assert.Equal(baseTime, time);
    }

    // ---------- 값 꺼내기 ----------

    [Fact]
    public void Parse_AllValues_ReturnsSixHours()
    {
        DailyWeather? weather = WeatherForecastClient.Parse(Response(), Today, out _);
        Assert.NotNull(weather);
        Assert.Equal(2, weather.MinTemperature);
        Assert.Equal(11, weather.MaxTemperature);
        Assert.Equal(WeatherForecastClient.TableTimes, weather.Hours.Select(h => h.Time));
        Assert.All(weather.Hours, h => Assert.Equal("맑음", h.Condition));
        Assert.Equal(10, weather.Hours[0].Temperature);
        Assert.Equal(20, weather.Hours[0].RainChance);
    }

    [Theory]
    [InlineData("1", "0", "맑음")]
    [InlineData("3", "0", "구름많음")]
    [InlineData("4", "0", "흐림")]
    [InlineData("4", "1", "비")]
    [InlineData("1", "2", "비/눈")]
    [InlineData("4", "3", "눈")]
    [InlineData("3", "4", "소나기")]
    public void Parse_Codes_ToCondition(string sky, string rain, string expected)
    {
        DailyWeather? weather = WeatherForecastClient.Parse(Response(sky: sky, rain: rain), Today, out _);
        Assert.Equal(expected, weather!.Hours[2].Condition);
    }

    [Theory]
    [InlineData("2", "0")]   // SKY 2는 없는 코드
    [InlineData("1", "5")]   // PTY 5(빗방울)는 초단기예보 코드
    [InlineData("", "0")]
    public void Parse_UnknownCode_Fails(string sky, string rain)
    {
        Assert.Null(WeatherForecastClient.Parse(Response(sky: sky, rain: rain), Today, out string reason));
        Assert.Contains("코드값 오류", reason);
    }

    [Theory]
    [InlineData("-0.4", 0)]
    [InlineData("-0.5", -1)]
    [InlineData("-1.5", -2)]
    [InlineData("1.4", 1)]
    [InlineData("1.5", 2)]
    public void Parse_Temperature_Rounded(string min, int expected)
    {
        Assert.Equal(expected, WeatherForecastClient.Parse(Response(min: min), Today, out _)!.MinTemperature);
    }

    [Theory]
    [InlineData("TMP", "2100")]
    [InlineData("POP", "0600")]
    [InlineData("SKY", "1200")]
    [InlineData("PTY", "1500")]
    [InlineData("TMN", "0600")]
    [InlineData("TMX", "1500")]
    public void Parse_MissingValue_Fails(string category, string time)
    {
        Assert.Null(WeatherForecastClient.Parse(Response(skip: (category, time)), Today, out _));
    }

    [Fact]
    public void Parse_OtherDayOnly_Fails()
    {
        // 내일 값만 있으면 오늘 값이 없는 것이다.
        Assert.Null(WeatherForecastClient.Parse(Response(), Today.AddDays(-1), out _));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    public void Parse_NotNumber_Fails(string value)
    {
        Assert.Null(WeatherForecastClient.Parse(Response(min: value), Today, out _));
    }

    [Theory]
    [InlineData("03")]
    [InlineData("30")]
    public void Parse_ResultCodeNotOk_Fails(string code)
    {
        Assert.Null(WeatherForecastClient.Parse(Response(resultCode: code), Today, out string reason));
        Assert.Equal($"resultCode {code}", reason);
    }

    [Fact]
    public void Parse_Xml_Fails()
    {
        string xml = "<OpenAPI_ServiceResponse><cmmMsgHeader><returnReasonCode>30</returnReasonCode></cmmMsgHeader></OpenAPI_ServiceResponse>";
        Assert.Null(WeatherForecastClient.Parse(xml, Today, out string reason));
        Assert.Equal("응답 형식 오류", reason);
    }

    // ---------- 예보 받기 (HTTP) ----------

    [Fact]
    public async Task Client_Success_SendsRequestAndCaches()
    {
        using var context = new TestScope();
        var handler = new FakeHandler(_ => Ok(Response()));
        WeatherForecastClient client = CreateClient(context, handler, "key/+=");

        Assert.NotNull(await client.GetTodayAsync(Seoul, CancellationToken.None));
        Assert.NotNull(await client.GetTodayAsync(Seoul, CancellationToken.None));

        Assert.Single(handler.Requests);
        string url = handler.Requests[0].ToString();
        Assert.Contains("serviceKey=key%2F%2B%3D", url);
        Assert.Contains("base_date=20261112&base_time=0200&nx=60&ny=127", url);
        Assert.Contains("dataType=JSON", url);
    }

    [Fact]
    public async Task Client_OtherRegion_NotFromCache()
    {
        using var context = new TestScope();
        var handler = new FakeHandler(_ => Ok(Response()));
        WeatherForecastClient client = CreateClient(context, handler);

        await client.GetTodayAsync(Seoul, CancellationToken.None);
        await client.GetTodayAsync(Busan, CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Client_Failure_NotCached()
    {
        using var context = new TestScope();
        var handler = new FakeHandler(_ => Ok(Response(resultCode: "03")));
        WeatherForecastClient client = CreateClient(context, handler);

        Assert.Null(await client.GetTodayAsync(Seoul, CancellationToken.None));
        Assert.Null(await client.GetTodayAsync(Seoul, CancellationToken.None));

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Client_HttpError_ReturnsNull()
    {
        using var context = new TestScope();
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        Assert.Null(await CreateClient(context, handler).GetTodayAsync(Seoul, CancellationToken.None));
    }

    [Fact]
    public async Task Client_Timeout_ReturnsNull()
    {
        using var context = new TestScope();
        var handler = new FakeHandler(_ => throw new TaskCanceledException("시간 초과", new TimeoutException()));
        Assert.Null(await CreateClient(context, handler).GetTodayAsync(Seoul, CancellationToken.None));
    }

    [Fact]
    public async Task Client_ConnectionError_ReturnsNull()
    {
        using var context = new TestScope();
        var handler = new FakeHandler(_ => throw new HttpRequestException("연결 실패"));
        Assert.Null(await CreateClient(context, handler).GetTodayAsync(Seoul, CancellationToken.None));
    }

    [Fact]
    public async Task Client_AppStopping_Throws()
    {
        using var context = new TestScope();
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        var handler = new FakeHandler(_ => throw new TaskCanceledException());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateClient(context, handler).GetTodayAsync(Seoul, cancel.Token));
    }

    // ---------- 지도 그리기 ----------

    [Fact]
    public void Map_Renders_PngUnder150Kb()
    {
        var regions = new List<(WeatherRegion, DailyWeather?)> { (Seoul, Weather("비")), (Busan, null) };
        byte[]? png = Renderer().Render(Today, "서울", regions);

        Assert.NotNull(png);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png.Take(4));
        Assert.InRange(png.Length, 1, 150 * 1024);
    }

    [Fact]
    public void Map_AllConditions_Renders()
    {
        string[] conditions = ["맑음", "구름많음", "흐림", "비", "소나기", "비/눈", "눈"];
        var regions = conditions
            .Select((c, i) => (new WeatherRegion { Name = $"지역{i}", Lat = 37 - i * 0.5, Lon = 127 }, (DailyWeather?)Weather(c)))
            .ToList();
        Assert.NotNull(Renderer().Render(Today, "지역0", regions));
    }

    [Fact]
    public void Map_ToPoint_Corners()
    {
        Assert.Equal(0, WeatherMapRenderer.ToPoint(124.5, 38.7).X, 3);
        Assert.Equal(50, WeatherMapRenderer.ToPoint(124.5, 38.7).Y, 3);
        Assert.Equal(599.83, WeatherMapRenderer.ToPoint(132.2, 33.0).X, 1);
        Assert.Equal(598.34, WeatherMapRenderer.ToPoint(132.2, 33.0).Y, 1);
    }

    // ---------- 본문 만들기 ----------

    [Fact]
    public async Task Builder_Success_MapTableAndAttribution()
    {
        var client = new FakeForecastClient { [Seoul] = Weather("비"), [Busan] = null };
        var builder = new WeatherContentBuilder(Seoul, [Seoul, Busan], client, Renderer());

        string? html = await builder.BuildAsync("날씨 정보(서울)", Today, CancellationToken.None);

        Assert.NotNull(html);
        Assert.Equal("날씨 정보(서울)", builder.GubunName);
        Assert.Contains("<img src=\"data:image/png;base64,", html);
        Assert.Contains("오늘(2026-11-12) 서울 날씨", html);
        Assert.Contains("최저 2°C / 최고 11°C", html);
        Assert.Equal(6, html.Split("<tr><td").Length - 1);
        Assert.Contains("06:00", html);
        Assert.Contains("21:00", html);
        Assert.Contains("공공누리 제1유형", html);
        Assert.Equal(2, client.Calls);
    }

    [Fact]
    public async Task Builder_OwnRegionFails_ReturnsNull()
    {
        var client = new FakeForecastClient { [Seoul] = null, [Busan] = Weather("맑음") };
        var builder = new WeatherContentBuilder(Seoul, [Seoul, Busan], client, Renderer());

        Assert.Null(await builder.BuildAsync("날씨 정보(서울)", Today, CancellationToken.None));
        Assert.Equal(1, client.Calls);
    }

    [Fact]
    public async Task Builder_EncodesRegionAndApiValues()
    {
        var region = new WeatherRegion { Name = "<b>x</b>", Lat = 37, Lon = 127 };
        var client = new FakeForecastClient { [region] = Weather("<script>") };
        string? html = await new WeatherContentBuilder(region, [region], client, Renderer())
            .BuildAsync("x", Today, CancellationToken.None);

        Assert.DoesNotContain("<b>x</b>", html);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;b&gt;", html);
    }

    [Fact]
    public async Task Resolver_PicksWeatherBuilderByGubunName()
    {
        var client = new FakeForecastClient { [Seoul] = Weather("맑음"), [Busan] = Weather("맑음") };
        var builders = new List<INewsletterContentBuilder>
        {
            new WeatherContentBuilder(Seoul, [Seoul, Busan], client, Renderer()),
            new WeatherContentBuilder(Busan, [Seoul, Busan], client, Renderer())
        };
        var resolver = new ContentBuilderResolver(builders, new DefaultContentBuilder());

        Assert.Contains("부산 날씨", await resolver.BuildAsync("날씨 정보(부산)", Today, CancellationToken.None));
        Assert.Contains("뉴스레터입니다.", await resolver.BuildAsync("날씨 정보(평양)", Today, CancellationToken.None));
        Assert.Contains("뉴스레터입니다.", await resolver.BuildAsync("날씨 정보", Today, CancellationToken.None));
    }

    // ---------- 메일 이미지 ----------

    [Fact]
    public void Mail_DataImage_ToCid()
    {
        byte[] bytes = [1, 2, 3];
        string html = $"<p>a</p><img src=\"data:image/png;base64,{Convert.ToBase64String(bytes)}\" alt=\"m\">";
        var body = new BodyBuilder();

        string result = MailService.EmbedDataImages(html, body.LinkedResources);

        MimePart image = Assert.IsType<MimePart>(Assert.Single(body.LinkedResources));
        Assert.Equal($"<p>a</p><img src=\"cid:{image.ContentId}\" alt=\"m\">", result);
        Assert.Equal("image/png", image.ContentType.MimeType);
        using var stream = new MemoryStream();
        Assert.NotNull(image.Content);
        image.Content.DecodeTo(stream);
        Assert.Equal(bytes, stream.ToArray());
    }

    [Fact]
    public void Mail_NoImage_Unchanged()
    {
        var body = new BodyBuilder();
        const string html = "<p>더 이상 받지 않으려면</p><img src=\"https://example.com/a.png\">";
        Assert.Equal(html, MailService.EmbedDataImages(html, body.LinkedResources));
        Assert.Empty(body.LinkedResources);
    }

    [Fact]
    public void Mail_TwoImages_TwoResources()
    {
        var body = new BodyBuilder();
        string img = "<img src=\"data:image/png;base64,AQID\">";
        MailService.EmbedDataImages(img + img, body.LinkedResources);
        Assert.Equal(2, body.LinkedResources.Count);
    }

    // ---------- 도우미 ----------

    private static WeatherMapRenderer Renderer() => new(NullLogger<WeatherMapRenderer>.Instance);

    private static DailyWeather Weather(string condition) =>
        new(2, 11, WeatherForecastClient.TableTimes.Select(t => new HourlyWeather(t, condition, 5, 30)).ToList());

    private static WeatherForecastClient CreateClient(TestScope context, FakeHandler handler, string key = "test-key")
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Weather:BaseUrl"] = "https://example.test/getVilageFcst",
                ["Weather:ServiceKey"] = key
            })
            .Build();
        return new WeatherForecastClient(new FakeHttpClientFactory(handler), new MemoryCache(new MemoryCacheOptions()),
            context.Clock, configuration, NullLogger<WeatherForecastClient>.Instance);
    }

    private static HttpResponseMessage Ok(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };

    // 오늘(2026-11-12) 06~21시 3시간 간격 값과 최저·최고 기온. skip은 빼고 만든다.
    private static string Response(
        string resultCode = "00", string sky = "1", string rain = "0", string min = "2.0", string max = "11.0",
        (string Category, string Time)? skip = null)
    {
        var items = new List<object>();
        void Add(string category, string time, string value)
        {
            if (skip != (category, time))
            {
                items.Add(new { baseDate = "20261112", baseTime = "0200", category, fcstDate = "20261112", fcstTime = time, fcstValue = value, nx = 60, ny = 127 });
            }
        }
        foreach (TimeOnly t in WeatherForecastClient.TableTimes)
        {
            string time = t.ToString("HHmm");
            Add("TMP", time, "10");
            Add("POP", time, "20");
            Add("SKY", time, sky);
            Add("PTY", time, rain);
            Add("REH", time, "45");
        }
        Add("TMN", "0600", min);
        Add("TMX", "1500", max);
        var response = new
        {
            response = new
            {
                header = new { resultCode, resultMsg = "NORMAL_SERVICE" },
                body = new { dataType = "JSON", items = new { item = items }, pageNo = 1, numOfRows = 1000, totalCount = items.Count }
            }
        };
        return JsonSerializer.Serialize(response);
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(respond(request));
        }
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class FakeForecastClient : Dictionary<WeatherRegion, DailyWeather?>, IWeatherForecastClient
    {
        public int Calls { get; private set; }

        public Task<DailyWeather?> GetTodayAsync(WeatherRegion region, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(TryGetValue(region, out DailyWeather? weather) ? weather : null);
        }
    }
}
