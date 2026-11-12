namespace NewsletterApp.Services.Contents;

// 종류 이름으로 본문 생성기를 고른다. 없으면 기본 본문 생성기를 쓴다.
public sealed class ContentBuilderResolver(
    IEnumerable<INewsletterContentBuilder> builders,
    DefaultContentBuilder defaultBuilder)
{
    // 본문 HTML. 종류별 생성기가 외부 데이터를 받지 못하면 null.
    public async Task<string?> BuildAsync(string gubunName, DateOnly kstDay, CancellationToken cancellationToken)
    {
        INewsletterContentBuilder? builder = builders.FirstOrDefault(b => b.GubunName == gubunName);
        if (builder is null)
        {
            return defaultBuilder.Build(gubunName, kstDay);
        }
        return await builder.BuildAsync(gubunName, kstDay, cancellationToken);
    }
}
