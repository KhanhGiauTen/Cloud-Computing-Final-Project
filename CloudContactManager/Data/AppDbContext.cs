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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TODO: Configure entity relationships and constraints
            // Configure User -> Customers one-to-many relationship
            modelBuilder.Entity<User>()
                .HasMany(u => u.Customers)
                .WithOne(c => c.User!)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
