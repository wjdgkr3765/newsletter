namespace NewsletterApp.Data.Entities;

// subscriptions: 회원의 종류 구독
public class Subscription
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long GubunId { get; set; }

    public DateTime CreateDate { get; set; }

    public User? User { get; set; }

    public Gubun? Gubun { get; set; }
}
