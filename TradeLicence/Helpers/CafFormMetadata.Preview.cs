using System;
using System.Globalization;
using System.Linq;
using TradeLicence.Models.Caf;

namespace TradeLicence.Helpers
{
    /// <summary>
    /// Preview-screen helpers that sit alongside the step/field definitions in
    /// CafFormMetadata.cs: the synthetic "Preview" step shown after the last
    /// real step, and value formatting for read-only display.
    /// </summary>
    public static partial class CafFormMetadata
    {
        /// <summary>
        /// The step number used by the stepper's Preview tab. It sits one past
        /// the last real step so the Preview link is never confused with a
        /// step that maps to a database table.
        /// </summary>
        public static int PreviewStep => MaxStep + 1;

        /// <summary>Label for the Preview tab in the stepper and for the Preview page title.</summary>
        public const string PreviewTitle = "Preview & Submit";

        /// <summary>
        /// Formats one stored value for the read-only Preview screen, or null
        /// when it is empty (the view then prints an em-dash). Select options
        /// may be encoded "value|label" (see the unitcategory field) and are
        /// stored as the value only, so they are mapped back to their label.
        /// </summary>
        public static string? DisplayValue(CafField field, string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            raw = raw.Trim();

            switch (field.Type)
            {
                case CafFieldType.Select:
                    var match = field.Options?.FirstOrDefault(o =>
                        string.Equals(o.Split('|')[0].Trim(), raw, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(match))
                    {
                        var pipe = match.IndexOf('|');
                        return pipe >= 0 ? match[(pipe + 1)..].Trim() : match;
                    }
                    return raw;

                case CafFieldType.Decimal:
                    return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
                        ? d.ToString("0.##", CultureInfo.InvariantCulture)
                        : raw;

                case CafFieldType.Date:
                    return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                        ? dt.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
                        : raw;

                default:
                    return raw;
            }
        }
    }
}
