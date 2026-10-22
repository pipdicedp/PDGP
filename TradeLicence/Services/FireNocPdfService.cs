using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeLicence.Helpers;
using TradeLicence.Interfaces;
using TradeLicence.Models.Caf;

namespace TradeLicence.Services
{
    /// <summary>
    /// QuestPDF documents for the Fire NOC service, mirroring
    /// TradeLicenceService.GenerateAcknowledgementPdfAsync / GenerateCertificatePdfAsync.
    /// Every field on the form is rendered from FireFormMetadata, so adding a
    /// field to the form automatically shows up on both PDFs.
    /// </summary>
    public class FireNocPdfService : IFireNocPdfService
    {
        private readonly IFireFormService _forms;

        public FireNocPdfService(IFireFormService forms)
        {
            _forms = forms;
        }

        public async Task<byte[]> GenerateAcknowledgementPdfAsync(long loginId)
        {
            var app = await _forms.GetApplicationAsync(loginId)
                ?? throw new InvalidOperationException("Fire NOC application not found.");

            var details = await BuildDetailsAsync(loginId);
            var uploadedDocs = await BuildDocumentListAsync(loginId);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("Fire NOC Application — Acknowledgement Slip")
                            .Bold().FontSize(16).FontColor("#1a3a52");
                        col.Item().AlignCenter().Text("Government of Puducherry — Fire Service Department")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor("#1a3a52");
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Spacing(12);

                        col.Item().Background("#f5f9fa").Padding(10).Row(row =>
                        {
                            row.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text("Application Number").FontSize(9).FontColor("#5a6b78");
                                c.Item().Text(app.ApplicationCode ?? "-").FontSize(12).SemiBold();
                            });

                            row.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text("Submitted Date").FontSize(9).FontColor("#5a6b78");
                                c.Item().Text(app.SubmittedDate.HasValue
                                    ? app.SubmittedDate.Value.ToLocalTime().ToString("dd-MM-yyyy hh:mm tt")
                                    : "-").FontSize(12).SemiBold();
                            });

                            row.ConstantItem(120).AlignRight().AlignMiddle().Background("#1a7f4b")
                                .PaddingVertical(4).PaddingHorizontal(10)
                                .Text(app.Status).FontColor("#ffffff").FontSize(10).SemiBold();
                        });

                        col.Item().Text("Application Details").Bold().FontSize(12).FontColor("#1a3a52");
                        col.Item().Element(c => AddDetailTable(c, details));

                        if (uploadedDocs.Count > 0)
                        {
                            col.Item().PaddingTop(10).Text("Uploaded Documents").Bold().FontSize(12).FontColor("#1a3a52");
                            col.Item().Element(c => AddDocumentTable(c, uploadedDocs));
                        }

                        col.Item().PaddingTop(15).Background("#FFF8E6").Padding(10).Text(
                            "Please keep this acknowledgement for your records. You can track the status of " +
                            "your application at any time using the Application Number above.")
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

        public async Task<byte[]> GenerateNocPdfAsync(long loginId)
        {
            var app = await _forms.GetApplicationAsync(loginId)
                ?? throw new InvalidOperationException("Fire NOC application not found.");

            if (!string.Equals(app.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The Fire Safety Clearance is available only for approved applications.");

            var details = await BuildDetailsAsync(loginId);
            var nocNumber = $"FIRE/NOC/{app.ApplicationCode ?? app.Id.ToString()}";
            var issueDate = (app.ModifiedDate ?? DateTime.UtcNow).ToLocalTime();
            var validUpto = issueDate.AddYears(3);

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
                        col.Item().AlignCenter().Text("Fire Service Department")
                            .FontSize(10).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(8).AlignCenter().Text("FIRE SAFETY CLEARANCE CERTIFICATE (NOC)")
                            .Bold().FontSize(16).FontColor("#c9a227");
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor("#c9a227");
                    });

                    page.Content().PaddingTop(20).Column(col =>
                    {
                        col.Spacing(14);

                        col.Item().Background("#f5f9fa").Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Certificate Number").FontSize(9).FontColor("#5a6b78");
                                c.Item().Text(nocNumber).FontSize(11).SemiBold();
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Date of Issue").FontSize(9).FontColor("#5a6b78");
                                c.Item().Text(issueDate.ToString("dd-MM-yyyy")).FontSize(11).SemiBold();
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Valid Upto").FontSize(9).FontColor("#5a6b78");
                                c.Item().Text(validUpto.ToString("dd-MM-yyyy")).FontSize(11).SemiBold();
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Status").FontSize(9).FontColor("#5a6b78");
                                c.Item().PaddingTop(2).Element(e =>
                                {
                                    e.AlignLeft().Background("#1a7f4b").PaddingVertical(3).PaddingHorizontal(10)
                                        .Text(app.Status).FontColor("#ffffff").FontSize(10).SemiBold();
                                });
                            });
                        });

                        col.Item().PaddingTop(4).Text(text =>
                        {
                            text.Span("This is to certify that Fire Safety Clearance (No Objection Certificate) has been granted to ")
                                .FontSize(11);
                            text.Span(app.Name ?? "the applicant").Bold().FontSize(11);
                            text.Span(" for the premises described below, subject to the provisions of the National Building Code and the applicable Fire Safety Rules.")
                                .FontSize(11);
                        });

                        col.Item().Element(c => AddDetailTable(c, details));

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
                            "This is a system-generated Fire Safety Clearance certificate.")
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

        /// <summary>Every saved field, in step order, formatted the way the Preview page shows it.</summary>
        private async Task<List<(string Label, string? Value)>> BuildDetailsAsync(long loginId)
        {
            var preview = await _forms.LoadPreviewAsync(loginId);
            var details = new List<(string, string?)>();

            foreach (var section in preview.Sections)
            {
                foreach (var f in section.Step.Fields)
                {
                    var shown = FireFormMetadata.DisplayValue(f, section.Values.GetValueOrDefault(f.Name));
                    details.Add((f.Label, shown));
                }
            }

            return details;
        }

        private static void AddDetailTable(IContainer container, List<(string Label, string? Value)> rows)
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

        /// <summary>Labels and sizes of the documents the applicant actually uploaded.</summary>
        private async Task<List<(string Label, long Bytes)>> BuildDocumentListAsync(long loginId)
        {
            var sizes = await _forms.GetDocumentSizesAsync(loginId);
            var rows = new List<(string, long)>();

            foreach (var def in FireFormMetadata.AllDocuments)
            {
                if (sizes.TryGetValue(def.Column, out var bytes) && bytes > 0)
                    rows.Add((def.Label, bytes));
            }

            return rows;
        }

        private static void AddDocumentTable(IContainer container, List<(string Label, long Bytes)> rows)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                });

                table.Cell().Background("#eef4f8").Padding(5).Text("Document").SemiBold();
                table.Cell().Background("#eef4f8").Padding(5).AlignRight().Text("Size").SemiBold();

                foreach (var (label, bytes) in rows)
                {
                    var size = bytes >= 1048576
                        ? (bytes / 1048576.0).ToString("0.0") + " MB"
                        : Math.Max(1, bytes / 1024) + " KB";

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(label);
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(size);
                }
            });
        }
    }
}
