using System.Security.Cryptography;
using System.Text;

namespace TradeLicence.Services
{
    /// <summary>
    /// Generates a simple visual CAPTCHA as inline SVG (no image library / GDI+
    /// dependency needed, works cross-platform). Each character gets a random
    /// rotation, vertical offset, and colour, plus background noise lines/dots,
    /// which is enough to defeat naive OCR/scraping without needing a third-party
    /// CAPTCHA service.
    ///
    /// SECURITY NOTE: the generated code is never sent to the browser as plain
    /// text/data anywhere except baked into the SVG glyph shapes — it is stored
    /// server-side (in Session) and compared there on submit. Do NOT return the
    /// code itself in any JSON/API response.
    /// </summary>
    public class CaptchaService
    {
        // Excludes visually ambiguous characters (0/O, 1/I/l) to keep the code readable.
        private const string AllowedChars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnopqrstuvwxyz0123456789";
        private const int CodeLength = 6;

        public string GenerateCode()
        {
            var bytes = RandomNumberGenerator.GetBytes(CodeLength);
            var sb = new StringBuilder(CodeLength);
            foreach (var b in bytes)
            {
                sb.Append(AllowedChars[b % AllowedChars.Length]);
            }
            return sb.ToString();
        }

        public string RenderSvg(string code)
        {
            const int width = 160, height = 56;
            var sb = new StringBuilder();
            sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}' width='{width}' height='{height}'>");
            sb.Append($"<rect width='{width}' height='{height}' fill='#F0EEE6'/>");

            // Noise lines
            for (int i = 0; i < 5; i++)
            {
                var x1 = RandomNumberGenerator.GetInt32(0, width); var y1 = RandomNumberGenerator.GetInt32(0, height);
                var x2 = RandomNumberGenerator.GetInt32(0, width); var y2 = RandomNumberGenerator.GetInt32(0, height);
                sb.Append($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='#C9C4B6' stroke-width='1'/>");
            }

            // Characters, each with random rotation/offset/colour
            var colours = new[] { "#0B3B37", "#0F5F58", "#C97A3A", "#3B4441" };
            var slotWidth = width / (double)code.Length;
            for (int i = 0; i < code.Length; i++)
            {
                var cx = (int)(slotWidth * i + slotWidth / 2);
                var cy = height / 2 + RandomNumberGenerator.GetInt32(-4, 5);
                var rotation = RandomNumberGenerator.GetInt32(-25, 26);
                var colour = colours[RandomNumberGenerator.GetInt32(0, colours.Length)];
                var fontSize = RandomNumberGenerator.GetInt32(24, 30);
                sb.Append($"<text x='{cx}' y='{cy}' fill='{colour}' font-size='{fontSize}' " +
                          $"font-family='IBM Plex Mono, monospace' font-weight='700' text-anchor='middle' " +
                          $"dominant-baseline='middle' transform='rotate({rotation} {cx} {cy})'>{code[i]}</text>");
            }

            // Noise dots
            for (int i = 0; i < 25; i++)
            {
                var x = RandomNumberGenerator.GetInt32(0, width); var y = RandomNumberGenerator.GetInt32(0, height);
                sb.Append($"<circle cx='{x}' cy='{y}' r='1' fill='#C9C4B6'/>");
            }

            sb.Append("</svg>");
            return sb.ToString();
        }
    }
}
