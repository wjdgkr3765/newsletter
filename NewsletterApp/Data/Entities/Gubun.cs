namespace NewsletterApp.Data.Entities;

// gubuns: 뉴스레터 종류
public class Gubun
{
    public long Id { get; set; }

    public string Name { get; set; } = "";

    public NewsletterConfig? Config { get; set; }
}
