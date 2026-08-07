using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Name as per PAN is required")]
        [Display(Name = "Name as per PAN")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "DOB")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "PAN number is required")]
        [RegularExpression(@"^[A-Za-z]{5}\d{4}[A-Za-z]{1}$", ErrorMessage = "Enter a valid PAN number")]
        [Display(Name = "PAN No")]
        public string PANNumber { get; set; } = null!;

        [Required(ErrorMessage = "Mobile number is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Mobile number must be 10 digits")]
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; } = null!;

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [Display(Name = "Email ID")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Username is required")]
        [StringLength(15, ErrorMessage = "Username cannot exceed 15 characters")]
        [Display(Name = "User Name")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "Password must be 6-15 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
