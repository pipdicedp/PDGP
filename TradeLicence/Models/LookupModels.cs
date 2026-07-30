namespace TradeLicence.Models
{
    public class Municipality
    {
        public int MunicipalityId { get; set; }
        public string MunicipalityName { get; set; } = null!;
    }

    public class Ward
    {
        public int WardId { get; set; }
        public int MunicipalityId { get; set; }
        public string WardName { get; set; } = null!;
    }

    public class Area
    {
        public int AreaId { get; set; }
        public int WardId { get; set; }
        public string AreaName { get; set; } = null!;
    }

    public class Street
    {
        public int StreetId { get; set; }
        public int AreaId { get; set; }
        public string StreetName { get; set; } = null!;
    }

    public class DoorNumberLookup
    {
        public int DoorNumberId { get; set; }
        public int StreetId { get; set; }
        public string DoorNumberValue { get; set; } = null!;
    }

    public class DocumentChecklistItem
    {
        public int DocumentItemId { get; set; }
        public string DocumentName { get; set; } = null!;
        public int DisplayOrder { get; set; }
    }
}
