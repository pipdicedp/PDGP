namespace TradeLicence.Models
{
    public class VerifyPanRequest
    {
        public string PanNumber { get; set; } = "";
        public string FullName { get; set; } = "";
        public DateTime DateOfBirth { get; set; }
    }

    public class SendOtpRequest
    {
        public string MobileNumber { get; set; } = "";
    }

    public class VerifyOtpRequest
    {
        public string MobileNumber { get; set; } = "";
        public string Otp { get; set; } = "";
    }

    public class SendEmailOtpRequest
    {
        public string Email { get; set; } = "";
    }

    public class VerifyEmailOtpRequest
    {
        public string Email { get; set; } = "";
        public string Otp { get; set; } = "";
    }
}
