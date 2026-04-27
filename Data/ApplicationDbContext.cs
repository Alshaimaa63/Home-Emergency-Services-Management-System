using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HomeServices.Models;

namespace HomeServices.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Database Tables
        public DbSet<Category> Categories { get; set; }
        public DbSet<Request> Requests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure the relationship between Request and Customer (User)
            builder.Entity<Request>()
                .HasOne(r => r.Customer)
                .WithMany(u => u.CustomerRequests)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents multiple cascade paths

            // Configure the relationship between Request and Provider (User)
            builder.Entity<Request>()
                .HasOne(r => r.Provider)
                .WithMany(u => u.ProviderTasks)
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents multiple cascade paths
        }
    }
}