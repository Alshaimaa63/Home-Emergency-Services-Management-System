using Azure.Core;
using System.ComponentModel.DataAnnotations;

namespace HomeServices.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم القسم مطلوب")]
        [StringLength(50)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public string? Icon { get; set; } // مسار الأيقونة

        public virtual ICollection<Request> Requests { get; set; }
    }
}