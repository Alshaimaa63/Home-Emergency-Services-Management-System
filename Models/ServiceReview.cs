using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeServices.Models
{
    public class ServiceReview
    {
        public int Id { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Review comment is required")]
        [StringLength(500)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // العميل الذي قام بالتقييم (المرسل)
        [Required]
        public string CustomerId { get; set; } = string.Empty;

        [ForeignKey("CustomerId")]
        public virtual ApplicationUser? Customer { get; set; }

        // مقدم الخدمة الذي استلم التقييم (المستقبل)
        [Required]
        public string ServiceProviderId { get; set; } = string.Empty;

        [ForeignKey("ServiceProviderId")]
        public virtual ApplicationUser? ServiceProvider { get; set; }


        [Required]
        public int RequestId { get; set; }

        [ForeignKey("RequestId")]
        public virtual Request? Request { get; set; }
    }
}