namespace NewsletterApp.Data.Entities;

// historys: 회원이 뉴스레터를 받은 기록 (받는 데 성공한 회원만)
public class History
{
    public long Id { get; set; }

    public long NewsletterId { get; set; }

    public long UserId { get; set; }

    public DateTime CreateDate { get; set; }

    public Newsletter? Newsletter { get; set; }

    public User? User { get; set; }
}
