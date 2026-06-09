using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MessagingPlatform.Models;

namespace MessagingPlatform.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageGroup> MessageGroups { get; set; }
    public DbSet<MessageGroupMember> MessageGroupMembers { get; set; }
    public DbSet<GroupMessage> GroupMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Message>(entity =>
        {
            entity.HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MessageGroup>(entity =>
        {
            entity.HasOne(g => g.CreatedBy)
                .WithMany(u => u.CreatedGroups)
                .HasForeignKey(g => g.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MessageGroupMember>(entity =>
        {
            entity.HasIndex(m => new { m.GroupId, m.UserId }).IsUnique();

            entity.HasOne(m => m.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GroupMessage>(entity =>
        {
            entity.HasOne(gm => gm.Sender)
                .WithMany(u => u.GroupMessages)
                .HasForeignKey(gm => gm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(gm => gm.Group)
                .WithMany(g => g.Messages)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
