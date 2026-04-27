using System.ComponentModel.DataAnnotations;

namespace HomeServices.Models
{
    public class Request
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "يرجى وصف المشكلة")]
        [MinLength(10)]
        public string Description { get; set; }

        [Required]
        public DateTime PreferredSchedule { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Accepted, Completed, Cancelled

        public int? Rating { get; set; }
        public string? FeedbackComment { get; set; }

        // العلاقات
        [Required]
        public string CustomerId { get; set; }
        public virtual ApplicationUser Customer { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        public string? ProviderId { get; set; }
        public virtual ApplicationUser Provider { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}