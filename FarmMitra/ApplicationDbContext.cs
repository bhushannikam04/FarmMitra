using FarmMitra.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FarmMitra.Models;

namespace FarmMitra.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Complaint> Complaints { get; set; }
    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<PriceHistory> PriceHistories { get; set; } = null!;
    public DbSet<PlatformSetting> PlatformSettings { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(u => u.ApprovalStatus).HasDefaultValue("Approved");
        });

        builder.Entity<Product>()
            .HasOne(p => p.Farmer).WithMany()
            .HasForeignKey(p => p.FarmerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>()
            .HasOne(o => o.Buyer).WithMany()
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OrderItem>()
            .HasOne(i => i.Product).WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CartItem>()
            .HasOne(c => c.Product).WithMany()
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Complaint>()
            .HasOne(c => c.User).WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Conversation>(e =>
        {
            e.HasOne(c => c.Buyer).WithMany().HasForeignKey(c => c.BuyerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Farmer).WithMany().HasForeignKey(c => c.FarmerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Product).WithMany().HasForeignKey(c => c.ProductId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(c => new { c.BuyerId, c.FarmerId }).IsUnique();
        });

        builder.Entity<Message>(e =>
        {
            e.HasOne(m => m.Conversation).WithMany(c => c.Messages).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(m => new { m.ConversationId, m.SentAt });
        });
    }
}