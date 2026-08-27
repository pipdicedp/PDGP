using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    /// <summary>
    /// Input model for OfficerController.CreateUser (the Admin-only
    /// "User Creation" page). Mirrors the Officers table, with a few
    /// deliberate differences from a 1:1 field mapping:
    ///
    ///   - PasswordHash is never taken as direct input. The admin types a
    ///     plain Password (+ ConfirmPassword) here, and the controller runs
    ///     it through the same PasswordHasher&lt;Officer&gt; the login pages use
    ///     to produce PasswordHash — typing/pasting a hash directly would
    ///     produce an account nobody could ever log into.
    ///   - OfficerId (identity column), FailedLoginAttempts, LastLoginDate
    ///     and CreatedDate are system-managed and meaningless for an
    ///     account that hasn't been used yet, so they aren't inputs either;
    ///     the controller sets them (0, null, DateTime.UtcNow).
    ///   - CreatedBy is filled in automatically from the logged-in admin's
    ///     own username rather than typed, so it can't be spoofed.
    /// </summary>
    public class OfficerCreateViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm the password")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [StringLength(150)]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Designation is required")]
        [StringLength(100)]
        public string Designation { get; set; } = string.Empty;

        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string? Email { get; set; }

        [Display(Name = "Lock this account immediately")]
        public bool IsLocked { get; set; }
    }
}
