using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NewsletterApp.Data.Entities;

namespace NewsletterApp.Data;

// 테이블·컬럼 매핑은 docs/database.md "4. 실제 사용할 테이블 생성 코드"와 같아야 한다.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Gubun> Gubuns => Set<Gubun>();

    public DbSet<NewsletterConfig> NewsletterConfigs => Set<NewsletterConfig>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<Newsletter> Newsletters => Set<Newsletter>();

    public DbSet<History> Historys => Set<History>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        bool isSqlServer = Database.IsSqlServer();

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LoginId).HasColumnName("login_id").HasMaxLength(20).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(60).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(30).IsRequired();
            entity.Property(e => e.IsAdmin).HasColumnName("is_admin").HasDefaultValue(false);
            entity.Property(e => e.IsSuspended).HasColumnName("is_suspended").HasDefaultValue(false);
            ConfigureCreateDate(entity.Property(e => e.CreateDate), isSqlServer);
            entity.HasIndex(e => e.LoginId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Gubun>(entity =>
        {
            entity.ToTable("gubuns");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<NewsletterConfig>(entity =>
        {
            entity.ToTable("newsletter_configs");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SendTime).HasColumnName("send_time");
            entity.Property(e => e.GubunId).HasColumnName("gubun_id");
            entity.Property(e => e.IsUse).HasColumnName("is_use").HasDefaultValue(false);
            if (isSqlServer)
            {
                entity.Property(e => e.SendTime).HasColumnType("time(0)");
            }
            entity.HasOne(e => e.Gubun).WithOne(g => g.Config).HasForeignKey<NewsletterConfig>(e => e.GubunId);
            entity.HasIndex(e => e.GubunId).IsUnique();
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.GubunId).HasColumnName("gubun_id");
            ConfigureCreateDate(entity.Property(e => e.CreateDate), isSqlServer);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Gubun).WithMany().HasForeignKey(e => e.GubunId);
            entity.HasIndex(e => new { e.UserId, e.GubunId }).IsUnique();
            entity.HasIndex(e => e.GubunId).HasDatabaseName("idx_subscriptions_gubun_id");
        });

        modelBuilder.Entity<Newsletter>(entity =>
        {
            entity.ToTable("newsletters");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.GubunId).HasColumnName("gubun_id");
            entity.Property(e => e.SendDay).HasColumnName("send_day");
            ConfigureCreateDate(entity.Property(e => e.CreateDate), isSqlServer);
            if (isSqlServer)
            {
                entity.Property(e => e.SendDay).HasColumnType("date");
            }
            entity.HasOne(e => e.Gubun).WithMany().HasForeignKey(e => e.GubunId);
            entity.HasIndex(e => new { e.GubunId, e.SendDay }).IsUnique();
            entity.HasIndex(e => e.CreateDate).HasDatabaseName("idx_newsletters_create_date");
        });

        modelBuilder.Entity<History>(entity =>
        {
            entity.ToTable("historys");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NewsletterId).HasColumnName("newsletter_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            ConfigureCreateDate(entity.Property(e => e.CreateDate), isSqlServer);
            entity.HasOne(e => e.Newsletter).WithMany(n => n.Historys).HasForeignKey(e => e.NewsletterId);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            entity.HasIndex(e => new { e.NewsletterId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_historys_user_id");
        });

        // database.md: ON DELETE CASCADE를 쓰지 않는다. 자식부터 코드에서 지운다.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    // DATETIME 컬럼은 UTC로 저장하고, 읽을 때 Kind를 UTC로 지정한다.
    private static void ConfigureCreateDate(
        Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<DateTime> property,
        bool isSqlServer)
    {
        property.HasColumnName("create_date")
            .HasConversion(new ValueConverter<DateTime, DateTime>(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc)));
        if (isSqlServer)
        {
            property.HasColumnType("datetime").HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
