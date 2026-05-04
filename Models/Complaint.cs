using System.ComponentModel.DataAnnotations;

namespace HomeServices.Models
{
    public class Complaint
    {
        public int Id { get; set; }

        public string Status { get; set; } = "Pending";

        [Required(ErrorMessage = "Title is required")]
        [Display(Name = "Complaint Title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Details")]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [EmailAddress]
        [Display(Name = "User Email")]
        public string UserEmail { get; set; }
    }
}