using Azure.Core;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HomeServices.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "الاسم بالكامل")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "العنوان مطلوب")]
        [StringLength(200)]
        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // الربط مع الطلبات (العميل له طلبات، والفني له مهام)
        public virtual ICollection<Request> CustomerRequests { get; set; }
        public virtual ICollection<Request> ProviderTasks { get; set; }
    }
}