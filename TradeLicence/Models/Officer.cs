using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    /// <summary>
    /// Department official / staff account — separate from ApplicationUser
    /// (citizen applicants). Password is never stored in plain text, same
    /// PasswordHasher-based approach as ApplicationUser.
    /// </summary>
    public class Officer
    {
        [Key] public int OfficerId { get; set; }
        [Required, StringLength(100)] public string Username { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        [StringLength(150)] public string? FullName { get; set; }
        [StringLength(150)] public string? Department { get; set; }
        [StringLength(100)] public string? Designation { get; set; }
        [StringLength(150)] public string? Email { get; set; }
        public bool IsLocked { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        [StringLength(100)] public string? CreatedBy { get; set; }
    }

    public class OfficerLoginViewModel
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
