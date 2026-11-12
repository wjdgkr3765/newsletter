namespace NewsletterApp.Data.Entities;

// newsletter_configs: 종류별 발송 설정 (종류당 하나)
public class NewsletterConfig
{
    public long Id { get; set; }

    // 한국 시간 시·분
    public TimeOnly SendTime { get; set; }

    public long GubunId { get; set; }

    public bool IsUse { get; set; }

    public Gubun? Gubun { get; set; }
}
