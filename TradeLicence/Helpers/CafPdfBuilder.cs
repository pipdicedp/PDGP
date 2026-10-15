using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeLicence.Models.Caf;

namespace TradeLicence.Helpers
{
    /// <summary>
    /// The two CAF PDFs, built from the same CafPreviewViewModel the Application Preview page shows (so a PDF can
    /// never disagree with the screen):
    ///   - BuildFullApplication: every section, sub-table and document slot (applicant "Download PDF" and the
    ///     officers' "Download PDF").
    ///   - BuildAcknowledgement: a one-page slip with only the basic, important details (applicant only).
    /// Static on purpose — no service registration needed. QuestPDF's licence is set once in Program.cs.
    /// </summary>
    public static class CafPdfBuilder
    {
        private const string Navy = "#1a3a52";
        private const string Department = "Government of Puducherry — Department of Industries and Commerce";

        // ---------------------------------------------------------------- full application

        public static byte[] BuildFullApplication(CafPreviewViewModel preview, long loginId, DateTime? appliedOn)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => AddHeader(c, "Common Application Form"));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Element(c => AddInfoBand(c, loginId, appliedOn, preview.IsSubmitted));

                        foreach (var section in preview.Sections)
                        {
                            var step = section.Step;

                            col.Item().PaddingTop(6).Background(Navy).Padding(6)
                                .Text($"{step.Number}. {step.Title}").FontColor(Colors.White).SemiBold().FontSize(11);

                            // documents step: one row per document slot
                            if (step.Documents.Count > 0)
                            {
                                var docRows = section.Documents
                                    .Select((d, i) => new string?[]
                                    {
                                        (i + 1).ToString(),
                                        d.Def.Label + (d.Def.Required ? " *" : ""),
                                        d.HasFile
                                            ? $"Uploaded ({FormatSize(d.SizeBytes)})"
                                            : (d.Def.Required ? "Required - not uploaded" : "Not uploaded")
                                    })
                                    .ToList();

                                col.Item().Element(c => AddListTable(c, new[] { "#", "Document", "Status" }, docRows, new float[] { 1, 8, 4 }));
                                continue;
                            }

                            if (!section.HasRow)
                            {
                                col.Item().Text("Not filled yet.").Italic().FontColor(Colors.Grey.Darken1);
                                continue;
                            }

                            foreach (var group in step.Fields.GroupBy(f => f.Section))
                            {
                                if (!string.IsNullOrWhiteSpace(group.Key))
                                    col.Item().PaddingTop(2).Text(group.Key).Bold().FontColor(Navy);

                                var rows = group
                                    .Select(f => (f.Label, CafFormMetadata.DisplayValue(f, section.Values.GetValueOrDefault(f.Name))))
                                    .ToArray();
                                col.Item().Element(c => AddDetailTable(c, rows));
                            }

                            foreach (var sub in section.SubTables)
                            {
                                col.Item().PaddingTop(4).Text(sub.Def.DisplayName).Bold().FontColor(Navy);

                                if (sub.Rows.Count == 0)
                                {
                                    col.Item().Text("No records added.").Italic().FontColor(Colors.Grey.Darken1);
                                    continue;
                                }

                                var headers = sub.Def.Fields.Select(f => f.Label).ToArray();
                                var subRows = sub.Rows
                                    .Select(r => sub.Def.Fields
                                        .Select(f => CafFormMetadata.DisplayValue(f, r.Values.GetValueOrDefault(f.Name)))
                                        .ToArray())
                                    .ToList();
                                col.Item().Element(c => AddListTable(c, headers, subRows, null, 8.5f));
                            }
                        }
                    });

                    page.Footer().AlignCenter().DefaultTextStyle(x => x.FontSize(8)).Text(t =>
                    {
                        t.Span("Generated on ");
                        t.Span(DateTime.Now.ToString("dd-MM-yyyy hh:mm tt"));
                        t.Span("   |   Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        // ---------------------------------------------------------------- acknowledgement slip

        public static byte[] BuildAcknowledgement(CafPreviewViewModel preview, long loginId, DateTime? appliedOn)
        {
            var documentSection = preview.Sections.FirstOrDefault(s => s.Step.Documents.Count > 0);
            var documentsText = documentSection == null
                ? "-"
                : $"{documentSection.Documents.Count(d => d.HasFile)} of {documentSection.Documents.Count} uploaded";

            var details = new (string, string?)[]
            {
                ("CAF Reference No.", loginId.ToString()),
                ("Name of Industry / Unit", Basic(preview, "indname")),
                ("Name of Promoter", Basic(preview, "promotername")),
                ("Unit Category", Basic(preview, "unitcategory")),
                ("Nature of Organisation", Basic(preview, "norganisationtype")),
                ("Nature of Application", Basic(preview, "natureofapp")),
                ("Nature of Activity", Basic(preview, "nactivityname")),
                ("Mobile Number", Basic(preview, "mobileno")),
                ("Documents", documentsText),
                ("Applied On", appliedOn?.ToString("dd-MM-yyyy hh:mm tt")),
                ("Status", preview.IsSubmitted ? "Submitted" : "Not submitted yet")
            };

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => AddHeader(c, "Common Application Form — Acknowledgement Slip"));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Spacing(12);

                        col.Item().Element(c => AddInfoBand(c, loginId, appliedOn, preview.IsSubmitted));

                        col.Item().Text("Application Details").Bold().FontSize(12).FontColor(Navy);
                        col.Item().Element(c => AddDetailTable(c, details));

                        col.Item().PaddingTop(15).Background("#FFF8E6").Padding(10).Text(
                            preview.IsSubmitted
                                ? "Please keep this acknowledgement for your records. You can track the status of your CAF anytime using the CAF Reference No. above."
                                : "This application has not been submitted yet. Submit it from the Application Preview page to complete your application.")
                            .FontSize(9).FontColor(Colors.Grey.Darken2);
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

        // ---------------------------------------------------------------- shared pieces

        // One value from the Basic Details section, formatted exactly as the preview shows it.
        private static string? Basic(CafPreviewViewModel preview, string fieldName)
        {
            var section = preview.Sections.FirstOrDefault(s =>
                string.Equals(s.Step.TableName, "caf_basic_details", StringComparison.OrdinalIgnoreCase));
            var field = section?.Step.Fields.FirstOrDefault(f =>
                string.Equals(f.Name, fieldName, StringComparison.OrdinalIgnoreCase));
            if (section == null || field == null) return null;

            return CafFormMetadata.DisplayValue(field, section.Values.GetValueOrDefault(field.Name));
        }

        private static string FormatSize(long? bytes)
        {
            if (!bytes.HasValue) return "";
            return bytes.Value < 1024 * 1024
                ? $"{Math.Max(1, bytes.Value / 1024)} KB"
                : $"{bytes.Value / 1024.0 / 1024.0:0.0} MB";
        }

        private static void AddHeader(IContainer container, string title)
        {
            container.Column(col =>
            {
                col.Item().AlignCenter().Text(title).Bold().FontSize(16).FontColor(Navy);
                col.Item().AlignCenter().Text(Department).FontSize(9).FontColor(Colors.Grey.Darken1);
                col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Navy);
            });
        }

        private static void AddInfoBand(IContainer container, long loginId, DateTime? appliedOn, bool isSubmitted)
        {
            container.Background("#f5f9fa").Padding(10).Row(row =>
            {
                row.RelativeItem(2).Column(c =>
                {
                    c.Item().Text("CAF Reference No.").FontSize(9).FontColor("#5a6b78");
                    c.Item().Text(loginId.ToString()).FontSize(12).SemiBold();
                });

                row.RelativeItem(2).Column(c =>
                {
                    c.Item().Text("Applied On").FontSize(9).FontColor("#5a6b78");
                    c.Item().Text(appliedOn.HasValue ? appliedOn.Value.ToString("dd-MM-yyyy hh:mm tt") : "-")
                        .FontSize(12).SemiBold();
                });

                row.ConstantItem(110).AlignRight().AlignMiddle()
                    .Background(isSubmitted ? "#1a7f4b" : "#D4A017")
                    .PaddingVertical(4).PaddingHorizontal(10)
                    .Text(isSubmitted ? "Submitted" : "Not submitted")
                    .FontColor("#ffffff").FontSize(10).SemiBold();
            });
        }

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
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(value ?? "-");
                }
            });
        }

        private static void AddListTable(IContainer container, string[] headers, IEnumerable<string?[]> rows,
                                         float[]? widths = null, float fontSize = 10)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    for (var i = 0; i < headers.Length; i++)
                        columns.RelativeColumn(widths != null && i < widths.Length ? widths[i] : 1);
                });

                table.Header(header =>
                {
                    foreach (var h in headers)
                        header.Cell().Background(Navy).Padding(5).Text(h).FontSize(fontSize).FontColor(Colors.White).SemiBold();
                });

                foreach (var row in rows)
                {
                    foreach (var cell in row)
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(cell ?? "-").FontSize(fontSize);
                }
            });
        }
    }
}
