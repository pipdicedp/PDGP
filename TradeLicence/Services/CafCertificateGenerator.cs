using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeLicence.Helpers;
using TradeLicence.Interfaces;
using TradeLicence.Models.Caf;

namespace TradeLicence.Services
{
    /// <summary>Everything a CAF certificate template may print - the template never touches the database.</summary>
    public class CafCertificateData
    {
        public long LoginId { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public string? Remarks { get; set; }

        public string? UnitName { get; set; }
        public string? PromoterName { get; set; }
        public string? UnitCategory { get; set; }
        public string? OrganisationType { get; set; }
        public string? NatureOfApplication { get; set; }
        public string? NatureOfActivity { get; set; }
        public string? Mobile { get; set; }
        public DateTime? AppliedOn { get; set; }

        /// <summary>Reads the unit details from the CAF preview (the same loader the acknowledgement slip uses).</summary>
        public static CafCertificateData From(CafPreviewViewModel preview, long loginId, string certificateNumber,
            DateTime issueDate, string? remarks, DateTime? appliedOn)
        {
            string? Basic(string fieldName)
            {
                var section = preview.Sections.FirstOrDefault(s =>
                    string.Equals(s.Step.TableName, "caf_basic_details", StringComparison.OrdinalIgnoreCase));
                var field = section?.Step.Fields.FirstOrDefault(f =>
                    string.Equals(f.Name, fieldName, StringComparison.OrdinalIgnoreCase));
                if (section == null || field == null) return null;
                return CafFormMetadata.DisplayValue(field, section.Values.GetValueOrDefault(field.Name));
            }

            return new CafCertificateData
            {
                LoginId = loginId,
                CertificateNumber = certificateNumber,
                IssueDate = issueDate,
                Remarks = remarks,
                AppliedOn = appliedOn,
                UnitName = Basic("indname"),
                PromoterName = Basic("promotername"),
                UnitCategory = Basic("unitcategory"),
                OrganisationType = Basic("norganisationtype"),
                NatureOfApplication = Basic("natureofapp"),
                NatureOfActivity = Basic("nactivityname"),
                Mobile = Basic("mobileno")
            };
        }
    }

    /// <summary>
    /// TEMPORARY certificate: the Trade Licence certificate layout (same border, header, info band,
    /// detail table and signature block as TradeLicenceService.GenerateCertificatePdfAsync) with CAF
    /// details. Replace this class when the department's real CAF certificate format is final.
    /// </summary>
    public class TradeLicenceStyleCafCertificateGenerator : ICafCertificateGenerator
    {
        public byte[] Generate(CafCertificateData d)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    page.Background().Border(3).BorderColor("#c9a227").Padding(6).Border(1).BorderColor("#1a3a52");

                    page.Header().PaddingTop(15).Column(col =>
                    {
                        col.Item().AlignCenter().Text("GOVERNMENT OF PUDUCHERRY")
                            .Bold().FontSize(14).FontColor("#1a3a52");
                        col.Item().AlignCenter().Text("Department of Industries and Commerce")
                            .FontSize(10).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(8).AlignCenter().Text("COMMON APPLICATION FORM CERTIFICATE")
                            .Bold().FontSize(18).FontColor("#c9a227");
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor("#c9a227");
                    });

                    page.Content().PaddingTop(20).Column(col =>
                    {
                        col.Spacing(14);

                        col.Item().Background("#f5f9fa").Padding(10).Row(row =>
                        {
                            row.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text("Certificate Number").FontSize(9).FontColor("#5a6b78");
                                c.Item().Text(d.CertificateNumber).FontSize(12).SemiBold();
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Date of Issue").FontSize(9).FontColor("#5a6b78");
                                c.Item().Text(d.IssueDate.ToString("dd-MM-yyyy")).FontSize(12).SemiBold();
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Status").FontSize(9).FontColor("#5a6b78");
                                c.Item().PaddingTop(2).Element(e =>
                                {
                                    e.AlignLeft().Background("#1a7f4b").PaddingVertical(3).PaddingHorizontal(10)
                                        .Text("Approved").FontColor("#ffffff").FontSize(10).SemiBold();
                                });
                            });
                        });

                        col.Item().PaddingTop(4).Text(text =>
                        {
                            text.Span("This is to certify that the Common Application Form (CAF Ref. No. ").FontSize(11);
                            text.Span(d.LoginId.ToString()).Bold().FontSize(11);
                            text.Span(") submitted by ").FontSize(11);
                            text.Span(Show(d.PromoterName)).Bold().FontSize(11);
                            text.Span(" for the industry / unit ").FontSize(11);
                            text.Span(Show(d.UnitName)).Bold().FontSize(11);
                            text.Span(" has been examined and APPROVED, subject to the provisions of the applicable Acts and Rules.").FontSize(11);
                        });

                        col.Item().Element(c => AddDetailTable(c, new (string, string?)[]
                        {
                            ("CAF Reference No.", d.LoginId.ToString()),
                            ("Name of Industry / Unit", d.UnitName),
                            ("Name of Promoter", d.PromoterName),
                            ("Unit Category", d.UnitCategory),
                            ("Nature of Organisation", d.OrganisationType),
                            ("Nature of Application", d.NatureOfApplication),
                            ("Nature of Activity", d.NatureOfActivity),
                            ("Mobile Number", d.Mobile),
                            ("Applied On", d.AppliedOn?.ToString("dd-MM-yyyy"))
                        }));

                        col.Item().PaddingTop(20).Row(row =>
                        {
                            row.RelativeItem();
                            row.ConstantItem(180).Column(c =>
                            {
                                c.Item().AlignCenter().PaddingBottom(30).Text("");
                                c.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                                c.Item().AlignCenter().Text("Issuing Authority").FontSize(9).FontColor(Colors.Grey.Darken2);
                            });
                        });

                        col.Item().PaddingTop(10).Background("#FFF8E6").Padding(8).Text(
                            "This is a system-generated placeholder certificate for demo purposes.")
                            .FontSize(8).Italic().FontColor(Colors.Grey.Darken2);
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Generated on ").FontSize(8);
                        t.Span(DateTime.Now.ToString("dd-MM-yyyy hh:mm tt")).FontSize(8);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static string Show(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

        private static void AddDetailTable(IContainer container, (string Label, string? Value)[] rows)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                });

                foreach (var (label, value) in rows)
                {
                    table.Cell().Background("#f5f9fa").Padding(5).Text(label).SemiBold();
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(Show(value));
                }
            });
        }
    }
}
