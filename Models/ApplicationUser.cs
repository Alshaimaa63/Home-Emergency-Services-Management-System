using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System;

namespace HomeServices.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Account Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Wallet Balance")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal WalletBalance { get; set; } = 1000.00m;

        [Display(Name = "About Me")]
        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string? Bio { get; set; }

        [Display(Name = "Specialty")]
        public string? Specialty { get; set; }

        // --- خاصية صورة البروفايل ---
        // بنخليها تحفظ اسم الملف فقط، وبنديها قيمة افتراضية عشان لو المستخدم مارفعش صورة
        [Display(Name = "Profile Picture")]
        public string? ProfilePicture { get; set; } = "default-user.png";

        // --- العلاقات (Relationships) ---
        // استخدمنا InverseProperty لو المشروع فيه علاقات معقدة (اختياري لكنه احترافي)

        public virtual ICollection<Request> CustomerRequests { get; set; } = new HashSet<Request>();
        public virtual ICollection<Request> ProviderTasks { get; set; } = new HashSet<Request>();
        public virtual ICollection<ServiceOffer> Offers { get; set; } = new HashSet<ServiceOffer>();

        // علاقات التقييمات
        public virtual ICollection<ServiceReview> ReviewsReceived { get; set; } = new HashSet<ServiceReview>();
        public virtual ICollection<ServiceReview> ReviewsGiven { get; set; } = new HashSet<ServiceReview>();
    }
}