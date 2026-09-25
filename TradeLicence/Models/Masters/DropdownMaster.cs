// File: Models/DropdownMaster.cs  (NEW FILE)
namespace TradeLicence.Models
{
    public class DropdownMaster
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;   // e.g. "ServiceCategory"
        public string Value { get; set; } = string.Empty;      // e.g. "Central Government"
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}