using System.ComponentModel.DataAnnotations;

namespace HomeServices.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "الإيميل مطلوب")]
        public string Email { get; set; }

        [Required(ErrorMessage = "كلمة السر مطلوبة")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}