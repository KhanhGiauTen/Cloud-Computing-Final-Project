using CloudContactManager.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudContactManager.Data
{
    /// <summary>
    /// Application database context for Entity Framework Core.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<CommunicationLog> CommunicationLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ================= FIX MYSQL DATA TYPES =================

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.Property(e => e.FullName).HasColumnType("varchar(100)");
                entity.Property(e => e.Address).HasColumnType("text");
                entity.Property(e => e.PhoneNumber).HasColumnType("varchar(20)");
                entity.Property(e => e.EmailAddress).HasColumnType("varchar(255)");
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Username).HasColumnType("varchar(50)");
                entity.Property(e => e.CompanyName).HasColumnType("varchar(200)");
                entity.Property(e => e.Email).HasColumnType("varchar(255)");
                entity.Property(e => e.PasswordHash).HasColumnType("text");
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.Property(e => e.PlanName).HasColumnType("varchar(100)");
            });

            modelBuilder.Entity<Campaign>(entity =>
            {
                entity.Property(e => e.CommunicationType).HasColumnType("varchar(50)");
                entity.Property(e => e.MessageContent).HasColumnType("text");
                entity.Property(e => e.SentAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<CommunicationLog>(entity =>
            {
                entity.Property(e => e.DeliveryStatus).HasColumnType("varchar(50)");
                entity.Property(e => e.ErrorMessage).HasColumnType("text");
                entity.Property(e => e.ExternalId).HasColumnType("varchar(255)");
            });

            // ================= RELATIONSHIPS =================

            modelBuilder.Entity<User>()
                .HasMany(u => u.Customers)
                .WithOne(c => c.User!)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SubscriptionPlan>()
                .HasMany(p => p.Tenants)
                .WithOne(u => u.Plan)
                .HasForeignKey(u => u.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany<Campaign>()
                .WithOne(c => c.User!)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Campaign>()
                .HasMany(c => c.CommunicationLogs)
                .WithOne(l => l.Campaign!)
                .HasForeignKey(l => l.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Customer>()
                .HasMany<CommunicationLog>()
                .WithOne(l => l.Customer!)
                .HasForeignKey(l => l.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
