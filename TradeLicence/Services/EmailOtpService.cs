using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace TradeLicence.Services
{
    public interface IOtpEmailSender
    {
        Task<bool> SendAsync(string toEmail, string subject, string body);
    }

    // TEST MODE: prints the email in Visual Studio (Output window / console, look for "DEV EMAIL").
    // Nothing is really sent. Use only while developing.
    public class ConsoleOtpEmailSender : IOtpEmailSender
    {
        private readonly ILogger<ConsoleOtpEmailSender> _logger;
        public ConsoleOtpEmailSender(ILogger<ConsoleOtpEmailSender> logger) => _logger = logger;

        public Task<bool> SendAsync(string toEmail, string subject, string body)
        {
            _logger.LogWarning("DEV EMAIL to {To} | {Subject} | {Body}", toEmail, subject, body);
            return Task.FromResult(true);
        }
    }

    // LIVE MODE: sends a real email through SMTP (for example Gmail).
    // Settings come from the "Email" section (see the guide).
    public class SmtpOtpEmailSender : IOtpEmailSender
    {
        private readonly IConfiguration _cfg;
        private readonly ILogger<SmtpOtpEmailSender> _logger;

        public SmtpOtpEmailSender(IConfiguration cfg, ILogger<SmtpOtpEmailSender> logger)
        {
            _cfg = cfg;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string toEmail, string subject, string body)
        {
            try
            {
                var host = _cfg["Email:Host"] ?? "smtp.gmail.com";
                var port = _cfg.GetValue<int?>("Email:Port") ?? 587;
                var user = _cfg["Email:User"];
                var pass = _cfg["Email:Password"];
                var from = _cfg["Email:From"] ?? user;

                if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass) || string.IsNullOrWhiteSpace(from))
                {
                    _logger.LogError("Email settings missing: set Email:User, Email:Password and Email:From.");
                    return false;
                }

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(user, pass)
                };

                using var msg = new MailMessage
                {
                    From = new MailAddress(from, "Puducherry Investor Portal"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };
                msg.To.Add(toEmail);

                await client.SendMailAsync(msg);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send OTP email to {To}", toEmail);
                return false;
            }
        }
    }

    // Generates, sends and checks EMAIL OTPs. State lives in the server-side Session
    // (same idea as the mobile OtpService, with its own "EOtp_" keys so they never mix).
    public class EmailOtpService
    {
        private const int ExpiryMinutes = 5;
        private const int MaxAttempts = 3;
        private const int ResendSeconds = 30;
        private const int MaxSendsPerSession = 5;

        private readonly IOtpEmailSender _email;
        public EmailOtpService(IOtpEmailSender email) => _email = email;

        private static string Norm(string? email) => (email ?? "").Trim().ToLowerInvariant();

        public async Task<(bool ok, string message)> SendAsync(ISession s, string emailRaw)
        {
            var email = Norm(emailRaw);

            if (long.TryParse(s.GetString("EOtp_LastSent"), out var lastTicks) &&
                DateTime.UtcNow < new DateTime(lastTicks, DateTimeKind.Utc).AddSeconds(ResendSeconds))
                return (false, $"Please wait {ResendSeconds} seconds before requesting another OTP.");

            if ((s.GetInt32("EOtp_Sends") ?? 0) >= MaxSendsPerSession)
                return (false, "Too many OTP requests. Please try again later.");

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var body =
                "Dear Applicant,\n\n" +
                $"Your OTP for Puducherry Investor Portal registration is {otp}.\n" +
                $"It is valid for {ExpiryMinutes} minutes. Do not share it with anyone.\n\n" +
                "Government of Puducherry - Investor Portal";

            if (!await _email.SendAsync(email, "Investor Portal - Email verification OTP", body))
                return (false, "Could not send the email. Please check the email address and try again.");

            s.SetString("EOtp_Email", email);
            s.SetString("EOtp_Hash", Hash(email, otp));
            s.SetString("EOtp_Expiry", DateTime.UtcNow.AddMinutes(ExpiryMinutes).Ticks.ToString());
            s.SetString("EOtp_LastSent", DateTime.UtcNow.Ticks.ToString());
            s.SetInt32("EOtp_Tries", 0);
            s.SetInt32("EOtp_Sends", (s.GetInt32("EOtp_Sends") ?? 0) + 1);
            s.Remove("EOtp_VerifiedEmail");

            return (true, $"OTP sent to {Mask(email)}. Valid for {ExpiryMinutes} minutes.");
        }

        public (bool ok, string message) Verify(ISession s, string emailRaw, string otp)
        {
            var email = Norm(emailRaw);
            var hash = s.GetString("EOtp_Hash");
            if (hash == null || s.GetString("EOtp_Email") != email)
                return (false, "Please request an OTP for this email address first.");

            if (!long.TryParse(s.GetString("EOtp_Expiry"), out var expiry) || DateTime.UtcNow.Ticks > expiry)
            {
                ClearOtp(s);
                return (false, "OTP has expired. Please request a new one.");
            }

            var tries = s.GetInt32("EOtp_Tries") ?? 0;
            if (tries >= MaxAttempts)
            {
                ClearOtp(s);
                return (false, "Too many wrong attempts. Please request a new OTP.");
            }

            var given = Encoding.UTF8.GetBytes(Hash(email, otp ?? ""));
            if (!CryptographicOperations.FixedTimeEquals(given, Encoding.UTF8.GetBytes(hash)))
            {
                s.SetInt32("EOtp_Tries", tries + 1);
                return (false, $"Incorrect OTP. {MaxAttempts - tries - 1} attempt(s) left.");
            }

            ClearOtp(s);
            s.SetString("EOtp_VerifiedEmail", email);
            return (true, "Email verified successfully.");
        }

        public bool IsVerified(ISession s, string? email)
            => !string.IsNullOrEmpty(email) && s.GetString("EOtp_VerifiedEmail") == Norm(email);

        public void ClearVerified(ISession s) => s.Remove("EOtp_VerifiedEmail");

        private static void ClearOtp(ISession s)
        {
            s.Remove("EOtp_Hash"); s.Remove("EOtp_Email"); s.Remove("EOtp_Expiry"); s.Remove("EOtp_Tries");
        }

        private static string Mask(string email)
        {
            var at = email.IndexOf('@');
            if (at < 1) return email;
            var name = email[..at];
            var shown = name.Length <= 2 ? name[..1] : name[..2];
            return shown + "****" + email[at..];
        }

        private static string Hash(string email, string otp)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{email}:{otp}")));
    }
}
