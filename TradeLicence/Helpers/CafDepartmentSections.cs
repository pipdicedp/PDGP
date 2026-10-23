using System;
using System.Collections.Generic;
using System.Linq;

namespace TradeLicence.Helpers
{
    /// <summary>
    /// What a RECEIVING department's officer may see of a forwarded CAF: Basic Details, that department's own
    /// step, and the uploaded documents — never the other departments' sections. The Industry officer who
    /// forwards the CAF still sees the full application.
    ///
    /// CafDepartmentApplication.Department holds the department master's deptname. The four departments that
    /// have their own CAF step are listed below; every other department (Fire Service, PWD, Legal Metrology, ...)
    /// has no step in the CAF form, so its officer gets Basic Details + documents.
    /// </summary>
    public static class CafDepartmentSections
    {
        // deptname exactly as spelled in dbo.eodbdeptmast  ->  key of the CAF step (CafFormMetadata) holding that
        // department's own details. Spacing and case are ignored when matching, the spelling itself is not.
        private static readonly Dictionary<string, string> OwnStepKeyByDepartment = new (string Name, string StepKey)[]
        {
            ("Town & Country Planning Department",                 "towncountry"),
            ("O/o The Chief Inspector of Factories and Boilers",   "factoryboiler"),
            ("Electricity Department",                             "electricity"),
            ("Department of Science, Technology & Environment",    "sciencetech"),
        }.ToDictionary(x => Normalize(x.Name), x => x.StepKey, StringComparer.OrdinalIgnoreCase);

        /// <summary>Step keys the department's officer may see (LoadPreviewAsync keeps the form's own step order).</summary>
        public static IReadOnlyCollection<string> StepKeysFor(string? department)
        {
            var name = Normalize(department);

            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Basic Details (the first step)
            var basic = CafFormMetadata.ByNumber(CafFormMetadata.MinStep);
            if (basic != null) keys.Add(basic.Key);

            // The department's own step: listed above, or a department named exactly like a step ("Electricity")
            var ownKey = OwnStepKeyByDepartment.TryGetValue(name, out var mapped)
                ? mapped
                : CafFormMetadata.Steps.FirstOrDefault(s => string.Equals(s.Title, name, StringComparison.OrdinalIgnoreCase))?.Key;
            if (ownKey != null) keys.Add(ownKey);

            // Every document (the documents step)
            foreach (var step in CafFormMetadata.Steps.Where(s => s.Documents.Count > 0))
                keys.Add(step.Key);

            return keys;
        }

        // "Registration  Department" (two spaces) and " Registration Department " compare equal.
        private static string Normalize(string? value) =>
            string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
