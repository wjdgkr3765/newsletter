namespace NewsletterApp.Data.Entities;

// users: 회원
public class User
{
    public long Id { get; set; }

    public string LoginId { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Email { get; set; } = "";

    public string Name { get; set; } = "";

    public bool IsAdmin { get; set; }

    public bool IsSuspended { get; set; }

    public DateTime CreateDate { get; set; }
}
