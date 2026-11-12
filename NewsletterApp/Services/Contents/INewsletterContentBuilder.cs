namespace NewsletterApp.Services.Contents;

// 종류별 본문 생성기. 외부 데이터를 받지 못하면 null을 돌려준다(그날은 보내지 않는다).
// 종류별 생성기를 만들면 Program.cs에 INewsletterContentBuilder로 등록한다.
public interface INewsletterContentBuilder
{
    // 이 생성기를 쓰는 종류 이름 (gubuns.name)
    string GubunName { get; }

    // HTML 본문. 외부 데이터는 HTML 인코딩해서 넣는다.
    Task<string?> BuildAsync(string gubunName, DateOnly kstDay, CancellationToken cancellationToken);
}
