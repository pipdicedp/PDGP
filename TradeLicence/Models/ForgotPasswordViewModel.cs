namespace TradeLicence.Models
{
    // All fields are optional here on purpose ("string?"): the controller checks them one by one
    // and shows a friendly message under each box.
    public class ForgotPasswordViewModel
    {
        // Step 1 : who are you
        public string? Username { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
        public string? CaptchaInput { get; set; }

        // Step 2 : prove it and set the new password
        public string? MobileOtp { get; set; }
        public string? EmailOtp { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}
