using LanChatServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LanChatServer.Data;

public class LanChatDb : DbContext
{
    public LanChatDb(DbContextOptions<LanChatDb> options) : base(options) { }

    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
    public DbSet<ChatUser> Users => Set<ChatUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FromUser).IsRequired();
            e.Property(x => x.ToUser).IsRequired();
            e.Property(x => x.Body).IsRequired();

            e.HasIndex(x => new { x.FromUser, x.ToUser, x.SentAtUtc });
            e.HasIndex(x => new { x.ToUser, x.DeliveredAtUtc });
        });

        modelBuilder.Entity<ChatUser>(e =>
        {
            e.HasKey(x => x.User);
            e.Property(x => x.User).IsRequired();
            e.Property(x => x.Machine).IsRequired();
            e.HasIndex(x => x.IsOnline);
            e.HasIndex(x => x.LastSeenUtc);
        });
    }
}
