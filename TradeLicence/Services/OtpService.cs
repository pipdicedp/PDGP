using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace TradeLicence.Services
{
    public interface ISmsSender
    {
        Task<bool> SendAsync(string mobile, string message);
    }

    // TEST MODE: prints the SMS in the terminal (look for "DEV SMS"). Never use in production.
    public class ConsoleSmsSender : ISmsSender
    {
        private readonly ILogger<ConsoleSmsSender> _logger;
        public ConsoleSmsSender(ILogger<ConsoleSmsSender> logger) => _logger = logger;

        public Task<bool> SendAsync(string mobile, string message)
        {
            _logger.LogWarning("DEV SMS to {Mobile}: {Message}", mobile, message);
            return Task.FromResult(true);
        }
    }

    // LIVE MODE: sends through your SMS gateway. Adjust the TODO lines to your gateway's documentation.
    public class HttpSmsSender : ISmsSender
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;

        public HttpSmsSender(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        public async Task<bool> SendAsync(string mobile, string message)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, _cfg["Sms:BaseUrl"]);
                req.Headers.Add("Authorization", _cfg["Sms:ApiKey"]);                // TODO header
                req.Content = JsonContent.Create(new                                  // TODO fields
                {
                    sender = _cfg["Sms:SenderId"],
                    templateId = _cfg["Sms:DltTemplateId"],
                    mobile = "91" + mobile,
                    message
                });
                var res = await _http.SendAsync(req);
                return res.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }

    // Generates, sends and checks OTPs. State lives in the server-side Session.
    public class OtpService
    {
        private const int ExpiryMinutes = 5;
        private const int MaxAttempts = 3;
        private const int ResendSeconds = 30;
        private const int MaxSendsPerSession = 5;

        private readonly ISmsSender _sms;
        public OtpService(ISmsSender sms) => _sms = sms;

        public async Task<(bool ok, string message)> SendAsync(ISession s, string mobile)
        {
            if (long.TryParse(s.GetString("Otp_LastSent"), out var lastTicks) &&
                DateTime.UtcNow < new DateTime(lastTicks, DateTimeKind.Utc).AddSeconds(ResendSeconds))
                return (false, $"Please wait {ResendSeconds} seconds before requesting another OTP.");

            if ((s.GetInt32("Otp_Sends") ?? 0) >= MaxSendsPerSession)
                return (false, "Too many OTP requests. Please try again later.");

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var text = $"Your OTP for Puducherry Investor Portal registration is {otp}. Valid for {ExpiryMinutes} minutes. Do not share it with anyone.";

            if (!await _sms.SendAsync(mobile, text))
                return (false, "Could not send OTP. Please try again.");

            s.SetString("Otp_Mobile", mobile);
            s.SetString("Otp_Hash", Hash(mobile, otp));
            s.SetString("Otp_Expiry", DateTime.UtcNow.AddMinutes(ExpiryMinutes).Ticks.ToString());
            s.SetString("Otp_LastSent", DateTime.UtcNow.Ticks.ToString());
            s.SetInt32("Otp_Tries", 0);
            s.SetInt32("Otp_Sends", (s.GetInt32("Otp_Sends") ?? 0) + 1);
            s.Remove("Otp_VerifiedMobile");

            return (true, $"OTP sent to +91 ******{mobile[^4..]}. Valid for {ExpiryMinutes} minutes.");
        }

        public (bool ok, string message) Verify(ISession s, string mobile, string otp)
        {
            var hash = s.GetString("Otp_Hash");
            if (hash == null || s.GetString("Otp_Mobile") != mobile)
                return (false, "Please request an OTP for this mobile number first.");

            if (!long.TryParse(s.GetString("Otp_Expiry"), out var expiry) || DateTime.UtcNow.Ticks > expiry)
            {
                ClearOtp(s);
                return (false, "OTP has expired. Please request a new one.");
            }

            var tries = s.GetInt32("Otp_Tries") ?? 0;
            if (tries >= MaxAttempts)
            {
                ClearOtp(s);
                return (false, "Too many wrong attempts. Please request a new OTP.");
            }

            var given = Encoding.UTF8.GetBytes(Hash(mobile, otp ?? ""));
            if (!CryptographicOperations.FixedTimeEquals(given, Encoding.UTF8.GetBytes(hash)))
            {
                s.SetInt32("Otp_Tries", tries + 1);
                return (false, $"Incorrect OTP. {MaxAttempts - tries - 1} attempt(s) left.");
            }

            ClearOtp(s);
            s.SetString("Otp_VerifiedMobile", mobile);
            return (true, "Mobile number verified successfully.");
        }

        public bool IsVerified(ISession s, string? mobile)
            => !string.IsNullOrEmpty(mobile) && s.GetString("Otp_VerifiedMobile") == mobile;

        public void ClearVerified(ISession s) => s.Remove("Otp_VerifiedMobile");

        private static void ClearOtp(ISession s)
        {
            s.Remove("Otp_Hash"); s.Remove("Otp_Mobile"); s.Remove("Otp_Expiry"); s.Remove("Otp_Tries");
        }

        private static string Hash(string mobile, string otp)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{mobile}:{otp}")));
    }
}
