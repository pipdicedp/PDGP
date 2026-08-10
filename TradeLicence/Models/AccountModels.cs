using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    /// <summary>
    /// Application user record. Password is never stored in plain text — only
    /// the PasswordHash (produced by ASP.NET Core's PasswordHasher, which
    /// internally salts + uses PBKDF2). See AccountController for hashing/verification.
    /// </summary>
    public class ApplicationUser
    {
        [Key] public int UserId { get; set; }
        [Required, StringLength(100)] public string Username { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        [StringLength(150)] public string? Email { get; set; }
        [StringLength(150)] public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [StringLength(10)] public string? PANNumber { get; set; }
        [StringLength(10)] public string? MobileNumber { get; set; }
        [StringLength(500)] public string? Address { get; set; }
        public bool IsLocked { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "User Name")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the code shown in the image")]
        [Display(Name = "Enter the code shown above")]
        public string CaptchaInput { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
