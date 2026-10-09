using System.Net.Http.Json;
using System.Text.Json;

namespace TradeLicence.Services
{
    public class PanVerificationResult
    {
        public bool Verified { get; set; }
        public string Message { get; set; } = "";
    }

    public interface IPanVerificationService
    {
        Task<PanVerificationResult> VerifyAsync(string pan, string fullName, DateTime dob);
    }

    // TEST MODE: accepts any correctly formatted PAN. Use only while developing.
    public class MockPanVerificationService : IPanVerificationService
    {
        public Task<PanVerificationResult> VerifyAsync(string pan, string fullName, DateTime dob)
            => Task.FromResult(new PanVerificationResult
            {
                Verified = true,
                Message = "PAN verified successfully (TEST MODE)."
            });
    }

    // LIVE MODE: calls your PAN verification provider.
    // Header names, request fields and response fields differ for every provider,
    // so the lines marked TODO must be changed using the provider's documentation.
    public class ApiPanVerificationService : IPanVerificationService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;

        public ApiPanVerificationService(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        public async Task<PanVerificationResult> VerifyAsync(string pan, string fullName, DateTime dob)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, _cfg["PanApi:BaseUrl"]);
                req.Headers.Add("x-api-key", _cfg["PanApi:ApiKey"]);                 // TODO header name
                req.Content = JsonContent.Create(new                                  // TODO field names
                {
                    pan,
                    name = fullName,
                    dob = dob.ToString("dd/MM/yyyy")
                });

                var res = await _http.SendAsync(req);
                if (!res.IsSuccessStatusCode)
                    return new PanVerificationResult { Message = "PAN verification service is unavailable. Please try again later." };

                using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                // TODO: replace these 3 lines with the real response fields of your provider
                bool panValid  = root.GetProperty("panValid").GetBoolean();
                bool nameMatch = root.GetProperty("nameMatch").GetBoolean();
                bool dobMatch  = root.GetProperty("dobMatch").GetBoolean();

                if (!panValid)  return new PanVerificationResult { Message = "This PAN is not valid or not active." };
                if (!nameMatch) return new PanVerificationResult { Message = "Name does not match the PAN records." };
                if (!dobMatch)  return new PanVerificationResult { Message = "Date of birth does not match the PAN records." };

                return new PanVerificationResult { Verified = true, Message = "PAN verified successfully." };
            }
            catch
            {
                return new PanVerificationResult { Message = "PAN verification service is unavailable. Please try again later." };
            }
        }
    }
}
