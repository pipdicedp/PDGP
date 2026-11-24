using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TradeLicence.Helpers
{
    /// <summary>
    /// The "No Objection Certificate" an applicant downloads once a department has APPROVED the CAF
    /// forwarded to it. Wording follows revenue_and_ppa.docx (one certificate per department; the
    /// applicant downloads each separately from the Track Status page).
    ///
    /// To add another department's approval later: add a line to <see cref="Templates"/> — the
    /// Track Status button and the download action pick it up automatically.
    /// Static on purpose (same as CafPdfBuilder). QuestPDF's licence is set once in Program.cs.
    /// </summary>
    public static class CafApprovalPdfBuilder
    {
        /// <summary>The certificate's heading lines and the phrase used in "...from the X point of view".</summary>
        private sealed record Template(string[] Heading, string PointOfView, string FileTag);

        // key = Officers.Department / CafDepartmentApplication.Department value
        private static readonly Dictionary<string, Template> Templates = new(StringComparer.OrdinalIgnoreCase)
        {
            [CafWorkflowConfig.RevenueDepartment] = new(
                new[] { "GOVERNMENT OF PUDUCHERRY", "REVENUE AND DISASTER MANAGEMENT", "(NO OBJECTION CERTIFICATE)" },
                "Revenue and Disaster Management",
                "Revenue"),

            // TCP wording follows the Revenue / Planning Authority certificates. If the department has an
            // official text, change the three values below (heading lines, "point of view" phrase, file tag).
            [CafWorkflowConfig.TownAndCountryPlanningDepartment] = new(
                new[] { "GOVERNMENT OF PUDUCHERRY", "TOWN AND COUNTRY PLANNING DEPARTMENT", "(NO OBJECTION CERTIFICATE)" },
                "Town and Country Planning",
                "TownCountryPlanning"),

            [CafWorkflowConfig.PlanningAuthorityDepartment] = new(
                new[] { "GOVERNMENT OF PUDUCHERRY", "PUDUCHERRY PLANNING AUTHORITY", "(NO OBJECTION CERTIFICATE)" },
                "Puducherry Planning Authority",
                "PlanningAuthority")
        };

        /// <summary>True if approving a CAF at this department gives the applicant a downloadable approval.</summary>
        public static bool HasApproval(string? department) =>
            !string.IsNullOrWhiteSpace(department) && Templates.ContainsKey(department.Trim());

        /// <summary>File-name friendly tag for the department, e.g. "Revenue".</summary>
        public static string FileTag(string department) =>
            Templates.TryGetValue(department.Trim(), out var t) ? t.FileTag : "Approval";

        /// <summary>
        /// Builds one department's approval PDF.
        /// </summary>
        /// <param name="department">Department the CAF was forwarded to (must satisfy <see cref="HasApproval"/>).</param>
        /// <param name="applicationNo">The EoDB / CAF reference number printed top-left.</param>
        /// <param name="approvedOnUtc">When the department approved (printed as the certificate date).</param>
        /// <param name="main">caf_basic_details row (column name -> value) of the applicant's CAF.</param>
        public static byte[] Build(string department, string applicationNo, DateTime approvedOnUtc,
                                   IReadOnlyDictionary<string, string?> main)
        {
            if (!Templates.TryGetValue(department.Trim(), out var tpl))
                throw new ArgumentException($"No approval certificate is defined for '{department}'.", nameof(department));

            string Get(string col) =>
                main.FirstOrDefault(kv => string.Equals(kv.Key, col, StringComparison.OrdinalIgnoreCase)).Value?.Trim() ?? string.Empty;

            static string Join(params string[] parts) =>
                string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim().TrimEnd(',')));

            var industry = Get("indname");
            var authorised = Get("authpersonname");
            if (string.IsNullOrEmpty(authorised)) authorised = Get("promotername");

            var commAddress = Join(Get("CommunicationAddress1"), Get("CommunicationAddress2"), Get("CommunicationAddress3"),
                                   Get("CommunicationDistrict"), Get("CommunicationState"), Get("pincode"));
            var promoterAddress = Join(Get("PromotorAddress1"), Get("PromotorAddress2"), Get("PromotorAddress3"),
                                       Get("PromotorDistrict"), Get("PromotorState"), Get("PromotorPinCode"));

            var dateText = ToIndiaTime(approvedOnUtc).ToString("dd-MM-yyyy");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2.54f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12).LineHeight(1.5f));

                    page.Content().Column(col =>
                    {
                        col.Spacing(14);

                        // heading
                        col.Item().AlignCenter().Column(h =>
                        {
                            foreach (var line in tpl.Heading)
                                h.Item().AlignCenter().Text(line).Bold().FontSize(13);
                        });

                        // application no (left) / place + date (right)
                        col.Item().PaddingTop(10).Row(r =>
                        {
                            r.RelativeItem().Text($"EoDB Application No.: {applicationNo}");
                            r.RelativeItem().AlignRight().Text($"Puducherry, dt. {dateText}");
                        });

                        // body
                        col.Item().PaddingTop(6).Text(t =>
                        {
                            t.Justify();
                            t.Span("This is to certify that the ");
                            t.Span(OrDash(industry));
                            t.Span(" of Mr./Ms. ");
                            t.Span(OrDash(authorised));
                            t.Span(" at ");
                            t.Span(OrDash(commAddress));
                            t.Span($" has been inspected and examined by the official of this Department from the {tpl.PointOfView} point of view. Hence, there is \u201CNo Objection\u201D.");
                        });

                        // signature block
                        col.Item().PaddingTop(60).AlignRight().Column(s =>
                        {
                            s.Item().AlignRight().Text("SIGNATURE OF SIGNING AUTHORITY").Bold();
                            s.Item().AlignRight().Text("PUDUCHERRY").Bold();
                        });

                        // addressee
                        col.Item().PaddingTop(30).Column(a =>
                        {
                            a.Item().Text("To");
                            a.Item().Text($"Mr./Ms. {OrDash(authorised)}");
                            a.Item().Text(OrDash(promoterAddress));
                        });
                    });
                });
            }).GeneratePdf();
        }

        private static string OrDash(string value) => string.IsNullOrWhiteSpace(value) ? "\u2014" : value;

        // The date on the certificate is the Indian date regardless of the server's time zone.
        private static DateTime ToIndiaTime(DateTime utc)
        {
            var asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            foreach (var id in new[] { "India Standard Time", "Asia/Kolkata" })
            {
                try { return TimeZoneInfo.ConvertTimeFromUtc(asUtc, TimeZoneInfo.FindSystemTimeZoneById(id)); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            return asUtc.AddHours(5.5);
        }
    }
}
