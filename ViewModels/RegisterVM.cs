using System.ComponentModel.DataAnnotations;

namespace HomeServices.ViewModels
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "Full Name is required")]

      
        [RegularExpression(@"^\s*[a-zA-Z\u0600-\u06FF]+\s+[a-zA-Z\u0600-\u06FF]+.*$",
         ErrorMessage = "Please enter at least two names (Letters only).")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Please enter a valid email address with a domain (e.g. .com)")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Phone Number is required")]
       
        [RegularExpression(@"^01[0125][0-9]{8}$",
        ErrorMessage = "Please enter a valid Egyptian phone number (11 digits starting with 010, 011, 012, or 015).")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(200, MinimumLength = 10, ErrorMessage = "Please enter a detailed address (at least 10 characters).")]
        [RegularExpression(@"^[a-zA-Z0-9\u0600-\u06FF\s,\-\/]+$",
        ErrorMessage = "Address contains invalid characters.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Please select an account type")]
        public string Role { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]

        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

    }
}