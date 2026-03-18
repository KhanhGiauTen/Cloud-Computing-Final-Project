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

            // Configure entity relationships and constraints

            // User (tenant) -> Customers one-to-many relationship
            modelBuilder.Entity<User>()
                .HasMany(u => u.Customers)
                .WithOne(c => c.User!)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // SubscriptionPlan -> Users (tenants) one-to-many
            modelBuilder.Entity<SubscriptionPlan>()
                .HasMany(p => p.Tenants)
                .WithOne(u => u.Plan)
                .HasForeignKey(u => u.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // User (tenant) -> Campaigns one-to-many
            modelBuilder.Entity<User>()
                .HasMany<Campaign>()
                .WithOne(c => c.User!)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Campaign -> CommunicationLogs one-to-many
            modelBuilder.Entity<Campaign>()
                .HasMany(c => c.CommunicationLogs)
                .WithOne(l => l.Campaign!)
                .HasForeignKey(l => l.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            // Customer -> CommunicationLogs one-to-many
            modelBuilder.Entity<Customer>()
                .HasMany<CommunicationLog>()
                .WithOne(l => l.Customer!)
                .HasForeignKey(l => l.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
