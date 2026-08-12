namespace TradeLicence.Models
{
    public class OfficerMenuItem
    {
        public string Text { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        // If set, this item renders as a dropdown — Controller/Action on the
        // parent itself are ignored and Children are shown instead.
        public List<OfficerMenuItem>? Children { get; set; }
    }
}
