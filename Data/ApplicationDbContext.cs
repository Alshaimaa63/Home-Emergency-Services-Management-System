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
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<ServiceOffer> ServiceOffers { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // تم توحيد الاسم لـ ReviewsReceived ليتناسب مع بقية الكود في المشروع
        public DbSet<ServiceReview> ReviewsReceived { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Relationship: Request and Customer
            builder.Entity<Request>()
                .HasOne(r => r.Customer)
                .WithMany(u => u.CustomerRequests)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Relationship: Request and Provider
            builder.Entity<Request>()
                .HasOne(r => r.Provider)
                .WithMany(u => u.ProviderTasks)
                .HasForeignKey(r => r.ServiceProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Bidding System Configuration
            builder.Entity<ServiceOffer>()
                .HasOne(so => so.Request)
                .WithMany(r => r.Offers)
                .HasForeignKey(so => so.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ServiceOffer>()
                .HasOne(so => so.Provider)
                .WithMany(u => u.Offers)
                .HasForeignKey(so => so.ServiceProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- 4. Reputation System Configuration (الأسماء المحدثة) ---

            // ربط التقييم بالعميل (الذي كتب التقييم)
            builder.Entity<ServiceReview>()
                .HasOne(sr => sr.Customer)
                .WithMany(u => u.ReviewsGiven)
                .HasForeignKey(sr => sr.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ربط التقييم بمقدم الخدمة (الذي استلم التقييم)
            // ملاحظة: تم تعديل sr.Provider ليكون sr.ServiceProvider ليتطابق مع الموديل الجديد
            builder.Entity<ServiceReview>()
                .HasOne(sr => sr.ServiceProvider)
                .WithMany(u => u.ReviewsReceived)
                .HasForeignKey(sr => sr.ServiceProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // ربط التقييم بالطلب (Cascade delete)
            builder.Entity<ServiceReview>()
                .HasOne(sr => sr.Request)
                .WithMany()
                .HasForeignKey(sr => sr.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}