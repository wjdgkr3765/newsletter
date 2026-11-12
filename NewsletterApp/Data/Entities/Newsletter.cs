namespace NewsletterApp.Data.Entities;

// newsletters: 종류별 하루 한 번 만든 뉴스레터
public class Newsletter
{
    public long Id { get; set; }

    public string Title { get; set; } = "";

    // HTML 본문 (수신 거부 링크 제외)
    public string Content { get; set; } = "";

    public long GubunId { get; set; }

    // 발송일 (한국 날짜)
    public DateOnly SendDay { get; set; }

    // 만든 시각 = 발송 시각 (UTC)
    public DateTime CreateDate { get; set; }

    public Gubun? Gubun { get; set; }

    public List<History> Historys { get; set; } = [];
}
