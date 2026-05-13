using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HomeServices.Models;
using Microsoft.AspNetCore.Identity;

namespace HomeServices.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 1. تعريف الجداول (DbSets)
        public DbSet<Category> Categories { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<ServiceOffer> ServiceOffers { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // تم توحيد الجدول ليكون سطر واحد فقط باسم ServiceReviews لضمان عدم التكرار
        public DbSet<ServiceReview> ServiceReviews { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // --- إعداد العلاقات (Relationships) ---

            // العلاقة بين الطلب والعميل
            builder.Entity<Request>()
                .HasOne(r => r.Customer)
                .WithMany(u => u.CustomerRequests)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // العلاقة بين الطلب ومقدم الخدمة
            builder.Entity<Request>()
                .HasOne(r => r.Provider)
                .WithMany(u => u.ProviderTasks)
                .HasForeignKey(r => r.ServiceProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // نظام المزايدات (Offers)
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

            // نظام التقييمات - ربط العميل (الذي يعطي التقييم)
            builder.Entity<ServiceReview>()
                .HasOne(sr => sr.Customer)
                .WithMany(u => u.ReviewsGiven)
                .HasForeignKey(sr => sr.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // نظام التقييمات - ربط مقدم الخدمة (الذي يستلم التقييم)
            builder.Entity<ServiceReview>()
                .HasOne(sr => sr.ServiceProvider)
                .WithMany(u => u.ReviewsReceived)
                .HasForeignKey(sr => sr.ServiceProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // الربط الصريح مع الطلب (الحل النهائي لمشكلة RequestId1)
            builder.Entity<ServiceReview>()
                .HasOne(sr => sr.Request)
                .WithMany(r => r.ServiceReviews)
                .HasForeignKey(sr => sr.RequestId)
                .OnDelete(DeleteBehavior.Cascade);


            // --- نظام زرع بيانات الأدمن (Admin Data Seeding) ---
            string adminRoleId = "9f7f9035-7d52-474c-836e-d90390f70154";
            string adminUserId = "b74ddd14-6340-4840-95c2-db12554843e5";

            builder.Entity<IdentityRole>().HasData(new IdentityRole
            {
                Id = adminRoleId,
                Name = "Admin",
                NormalizedName = "ADMIN"
            });

            var adminUser = new ApplicationUser
            {
                Id = adminUserId,
                UserName = "admin@home.com",
                NormalizedUserName = "ADMIN@HOME.COM",
                Email = "admin@home.com",
                NormalizedEmail = "ADMIN@HOME.COM",
                EmailConfirmed = true,
                FullName = "System Admin",
                IsVerified = true,
                CreatedAt = DateTime.Now,
                WalletBalance = 0.00m,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var passwordHasher = new PasswordHasher<ApplicationUser>();
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");

            builder.Entity<ApplicationUser>().HasData(adminUser);

            builder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                RoleId = adminRoleId,
                UserId = adminUserId
            });
        }
    }
}