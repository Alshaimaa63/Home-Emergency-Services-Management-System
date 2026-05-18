using System.ComponentModel.DataAnnotations;

namespace HomeServices.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(50, ErrorMessage = "Category name cannot exceed 50 characters")]
        [Display(Name = "Category Name")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Icon Path")]
        public string? Icon { get; set; }

        // Navigation property for related requests
        public virtual ICollection<Request> Requests { get; set; }
    }
}