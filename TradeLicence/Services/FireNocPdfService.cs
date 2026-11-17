using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QRCoder;
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
                        col.Item().AlignCenter().Text("Fire NOC Application â€” Acknowledgement Slip")
                            .Bold().FontSize(16).FontColor("#1a3a52");
                        col.Item().AlignCenter().Text("Government of Puducherry â€” Fire Service Department")
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

        private static byte[] GenerateQrPng(string text, int pixelsPerModule)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<byte>();

            using var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            return png.GetGraphic(pixelsPerModule);
        }

        public async Task<byte[]> GenerateNocPdfAsync(long loginId)
        {
            var app = await _forms.GetApplicationAsync(loginId)
                ?? throw new InvalidOperationException("Fire NOC application not found.");

            if (!string.Equals(app.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The Fire Safety Clearance is available only for approved applications.");

            var preview = await _forms.LoadPreviewAsync(loginId);
            var nocNumber = $"C.No.{app.ApplicationCode ?? app.Id.ToString()}/FSD/FPW/NOC/{DateTime.Now.Year}/";
            var issueDate = (app.ModifiedDate ?? DateTime.UtcNow).ToLocalTime();
            var validUpto = issueDate.AddYears(3);

            string? GetVal(string name) =>
                preview.Sections.SelectMany(s => s.Values.Where(v => string.Equals(v.Key, name, StringComparison.OrdinalIgnoreCase)))
                       .Select(v => v.Value)
                       .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            var buildingclass = GetVal("buildingclass") ?? "";
            var isEducational = buildingclass.Contains("Educational", StringComparison.OrdinalIgnoreCase);

            var name = app.Name ?? GetVal("name") ?? GetVal("ownername") ?? GetVal("premisesname") ?? "-";
            var address = GetVal("address") ?? GetVal("owneraddress") ?? "-";
            var floortype = GetVal("floortype") ?? "-";
            var occupencydetail = GetVal("occupencydetail") ?? "-";
            var firefighting = GetVal("firefighting") ?? "-";
            var hazard = GetVal("hazard") ?? "-";
            var builtuparea = GetVal("builtuparea") ?? "-";

            address = (address ?? "").Replace("\r", "").Replace("\n", "");
            occupencydetail = (occupencydetail ?? "").Replace("\r", "").Replace("\n", "");

            var qrText = $"Applicant Name :    {name}\nAddress :    {address}   \nApplication No. :    {(app.ApplicationCode ?? app.Id.ToString())}";
            var qrBytes = GenerateQrPng(qrText, 100);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(34);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor("#111111"));

                    page.Background().Padding(2)
                        .Border(2).BorderColor("#1a3a52");

                    // ---------------- letterhead ----------------
                    page.Header().PaddingHorizontal(16).PaddingTop(14).Column(col =>
                    {
                        col.Item().AlignCenter().Text("GOVERNMENT OF PUDUCHERRY")
                            .Bold().FontSize(13.5f).LetterSpacing(0.06f).FontColor("#1a3a52");
                        col.Item().AlignCenter().Text("FIRE SERVICE DEPARTMENT")
                            .Bold().FontSize(15).LetterSpacing(0.05f).FontColor("#1a3a52");
                        col.Item().AlignCenter().Text("Puducherry")
                            .FontSize(10).FontColor("#4b5d69");

                        col.Item().PaddingTop(8).LineHorizontal(1.4f).LineColor("#1a3a52");
                        col.Item().PaddingTop(2).LineHorizontal(0.8f).LineColor("#D4A017");

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Text(nocNumber).FontSize(10.5f).SemiBold();
                            row.RelativeItem().AlignRight().Text("Puducherry, dt. " + issueDate.ToString("dd-MM-yyyy"))
                                .FontSize(10.5f);
                        });
                    });

                    page.Content().PaddingHorizontal(16).PaddingTop(18).Column(col =>
                    {
                        col.Spacing(12);

                        // Different certificate formats by building classification
                        if (isEducational)
                        {
                            col.Item().AlignCenter().Text("NO OBJECTION CERTIFICATE")
                                .Bold().FontSize(16).Underline();

                            col.Item().PaddingTop(8).Text(text =>
                            {
                                text.Justify();
                                text.Span("           This is to certify that the ").FontSize(11.5f);
                                text.Span(floortype).FontSize(11.5f);
                                text.Span(" of M/s. ").FontSize(11.5f);
                                text.Span(name).SemiBold().FontSize(11.5f);
                                text.Span(" at ").FontSize(11.5f);
                                text.Span(address).FontSize(11.5f);
                                text.Span(" has been inspected and examined by the officials of this Department on Fire Safety point of view. Hence, there is").FontSize(11.5f);
                                text.Span(" 'No Objection'").SemiBold().FontSize(11.5f);
                                text.Span(" for the purpose of ").FontSize(11.5f);
                                text.Span(occupencydetail).FontSize(11.5f);
                                text.Span(".").FontSize(11.5f);
                            });
                        }
                        else
                        {
                            col.Item().AlignCenter().Text("FIRE SAFETY CLEARANCE CERTIFICATE")
                                .Bold().FontSize(15.5f).Underline();
                            col.Item().AlignCenter().Text("(No Objection Certificate)")
                                .FontSize(10.5f).FontColor("#4b5d69");

                            col.Item().PaddingTop(6).Element(c => AddNocParticulars(c,
                                ("Building Classification", string.IsNullOrWhiteSpace(buildingclass) ? "-" : buildingclass),
                                ("Type of Building", floortype),
                                ("Occupancy / Purpose", occupencydetail),
                                ("Built-up Area (sq.m.)", builtuparea),
                                ("Hazard Classification", hazard),
                                ("Fire Fighting Arrangements", firefighting),
                                ("Valid Upto", validUpto.ToString("dd-MM-yyyy"))));

                            col.Item().PaddingTop(4).Text(text =>
                            {
                                text.Justify();
                                text.Span("           This is to certify that the existing/proposed building premises of M/s. ").FontSize(11.5f);
                                text.Span(name).SemiBold().FontSize(11.5f);
                                text.Span(" situated at ").FontSize(11.5f);
                                text.Span(address).FontSize(11.5f);
                                text.Span(" has been inspected by the Fire Service Personnel and found to comply with the fire safety requirements as per the National Building Code of India and the Puducherry Fire Service Rules. Hence, there is ").FontSize(11.5f);
                                text.Span("'No Objection'").SemiBold().FontSize(11.5f);
                                text.Span(" for the purpose of ").FontSize(11.5f);
                                text.Span(occupencydetail).FontSize(11.5f);
                                text.Span(" subject to the observance of all fire safety measures and periodic maintenance of the fire fighting systems.").FontSize(11.5f);
                            });
                        }

                        // ---------------- signature ----------------
                        col.Item().PaddingTop(38).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                if (qrBytes.Length > 0)
                                {
                                    c.Item().Width(86).Height(86).Image(qrBytes);
                                    c.Item().PaddingTop(4).Text("Scan to verify").FontSize(7.5f)
                                        .FontColor("#4b5d69");
                                }
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("(K. ILANGO)").FontSize(11);
                                c.Item().Text("DIVISIONAL FIRE OFFICER").FontSize(11).SemiBold();
                                c.Item().Text("PUDUCHERRY").FontSize(11);
                            });
                        });

                        // ---------------- recipient ----------------
                        col.Item().PaddingTop(26).Column(c =>
                        {
                            c.Item().Text("To").FontSize(11);
                            c.Item().Text("Mr./Ms. " + name).FontSize(11).SemiBold();
                            c.Item().Text(address).FontSize(11);
                        });

                        col.Item().PaddingTop(14).Text(
                                "This certificate is issued on the basis of the particulars furnished in the application and the fire safety inspection conducted by this Department.")
                            .FontSize(8.5f).Italic().FontColor("#4b5d69");
                    });

                    page.Footer().PaddingHorizontal(16).PaddingBottom(10).AlignCenter().Text(t =>
                    {
                        t.Span("Generated on ").FontSize(8);
                        t.Span(DateTime.Now.ToString("dd-MM-yyyy hh:mm tt")).FontSize(8);
                        t.Span("  â€¢  Fire Service Department, Government of Puducherry").FontSize(8);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void AddNocParticulars(IContainer container, params (string Label, string Value)[] rows)
        {
            container.Border(0.8f).BorderColor("#cbd7df").Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.9f);
                });

                foreach (var (label, value) in rows)
                {
                    table.Cell().Background("#f4f7f9").Border(0.5f).BorderColor("#dbe4ea")
                        .Padding(5).Text(label).FontSize(9).SemiBold().FontColor("#33454f");
                    table.Cell().Border(0.5f).BorderColor("#dbe4ea")
                        .Padding(5).Text(value).FontSize(9.5f);
                }
            });
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


