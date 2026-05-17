using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using MedicalClinic.Models;

namespace MedicalClinic.Services
{
    /// <summary>
    /// REQ-37 + REQ-42: Generează PDF cu fișa medicală completă a pacientului.
    /// Folosește itext7 (compatibil .NET 10).
    /// </summary>
    public class MedicalRecordPdfService
    {
        public byte[] GeneratePatientRecordPdf(
            Patient patient,
            List<MedicalRecordEntry> records,
            List<Allergy> allergies,
            List<ExternalDocument> externalDocuments)
        {
            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, PageSize.A4);
            document.SetMargins(54f, 36f, 54f, 36f);

            // ---- Fonturi ----
            var fontBold   = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontNormal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            var colorPrimary = new DeviceRgb(30, 80, 160);
            var colorRed     = new DeviceRgb(180, 30, 30);
            var colorGray    = new DeviceRgb(100, 100, 100);
            var colorHeaderBg = new DeviceRgb(220, 230, 255);
            var colorAlertBg  = new DeviceRgb(255, 220, 220);

            // ---- Titlu ----
            document.Add(new Paragraph("MediClinic — Fișă Medicală")
                .SetFont(fontBold).SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(4));

            document.Add(new Paragraph($"Generat la: {DateTime.Now:dd/MM/yyyy HH:mm}")
                .SetFont(fontNormal).SetFontSize(9).SetFontColor(colorGray)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(8));

            // separator
            document.Add(new LineSeparator(new SolidLine(1f)).SetMarginBottom(10));

            // ---- Date pacient ----
            document.Add(new Paragraph("Date Pacient")
                .SetFont(fontBold).SetFontSize(12).SetFontColor(colorPrimary)
                .SetMarginTop(8).SetMarginBottom(4));

            var infoTable = new Table(new float[] { 1, 3 }).UseAllAvailableWidth();
            AddRow(infoTable, "Nume:", patient.Name, fontBold, fontNormal);
            AddRow(infoTable, "Absențe (No-Show):", patient.NoShowCount.ToString(), fontBold, fontNormal);
            document.Add(infoTable);

            // ---- Alergii (REQ-41) ----
            if (allergies.Any())
            {
                document.Add(new Paragraph("⚠ ALERGII CUNOSCUTE")
                    .SetFont(fontBold).SetFontSize(10).SetFontColor(colorRed)
                    .SetMarginTop(12).SetMarginBottom(4));

                var allergyTable = new Table(new float[] { 3, 1 }).UseAllAvailableWidth();

                allergyTable.AddHeaderCell(new Cell()
                    .Add(new Paragraph("Descriere").SetFont(fontBold).SetFontSize(9))
                    .SetBackgroundColor(colorAlertBg).SetPadding(4));
                allergyTable.AddHeaderCell(new Cell()
                    .Add(new Paragraph("Critic").SetFont(fontBold).SetFontSize(9))
                    .SetBackgroundColor(colorAlertBg).SetPadding(4));

                foreach (var a in allergies)
                {
                    allergyTable.AddCell(new Cell()
                        .Add(new Paragraph(a.Description ?? "").SetFont(fontNormal).SetFontSize(9))
                        .SetPadding(3));
                    allergyTable.AddCell(new Cell()
                        .Add(new Paragraph(a.IsCritical ? "DA" : "Nu")
                            .SetFont(fontBold).SetFontSize(9)
                            .SetFontColor(a.IsCritical ? colorRed : colorGray))
                        .SetPadding(3));
                }
                document.Add(allergyTable);
            }

            // ---- Istoric consultații ----
            document.Add(new Paragraph("Istoric Consultații")
                .SetFont(fontBold).SetFontSize(12).SetFontColor(colorPrimary)
                .SetMarginTop(14).SetMarginBottom(4));

            if (records.Any())
            {
                var recordTable = new Table(new float[] { 1.5f, 2.5f, 1f, 1f, 1f, 2.5f })
                    .UseAllAvailableWidth();

                foreach (var h in new[] { "Data", "Diagnostic", "Tensiune", "Greutate", "Temp.", "Note" })
                    recordTable.AddHeaderCell(new Cell()
                        .Add(new Paragraph(h).SetFont(fontBold).SetFontSize(8))
                        .SetBackgroundColor(colorHeaderBg).SetPadding(4));

                foreach (var r in records.OrderByDescending(x => x.Appointment?.StartTime))
                {
                    recordTable.AddCell(Cell9(r.Appointment?.StartTime.ToString("dd/MM/yyyy") ?? "-", fontNormal));
                    recordTable.AddCell(Cell9(r.Diagnoses ?? "", fontNormal));
                    recordTable.AddCell(Cell9(r.BloodPressure ?? "", fontNormal));
                    recordTable.AddCell(Cell9($"{r.Weight} kg", fontNormal));
                    recordTable.AddCell(Cell9($"{r.Temperature}°C", fontNormal));
                    recordTable.AddCell(Cell9(r.Notes ?? "", fontNormal));
                }
                document.Add(recordTable);
            }
            else
            {
                document.Add(new Paragraph("Nu există înregistrări medicale.")
                    .SetFont(fontNormal).SetFontSize(9).SetFontColor(colorGray));
            }

            // ---- Documente externe ----
            if (externalDocuments.Any())
            {
                document.Add(new Paragraph("Documente Externe")
                    .SetFont(fontBold).SetFontSize(12).SetFontColor(colorPrimary)
                    .SetMarginTop(14).SetMarginBottom(4));

                var docTable = new Table(new float[] { 2, 3 }).UseAllAvailableWidth();

                docTable.AddHeaderCell(new Cell()
                    .Add(new Paragraph("Tip Document").SetFont(fontBold).SetFontSize(9))
                    .SetBackgroundColor(colorHeaderBg).SetPadding(4));
                docTable.AddHeaderCell(new Cell()
                    .Add(new Paragraph("Cale Fișier").SetFont(fontBold).SetFontSize(9))
                    .SetBackgroundColor(colorHeaderBg).SetPadding(4));

                foreach (var doc in externalDocuments)
                {
                    docTable.AddCell(Cell9(doc.DocumentType ?? "", fontNormal));
                    docTable.AddCell(Cell9(doc.FilePath ?? "", fontNormal));
                }
                document.Add(docTable);
            }

            // ---- Footer ----
            document.Add(new LineSeparator(new SolidLine(1f)).SetMarginTop(14).SetMarginBottom(6));
            document.Add(new Paragraph("Document generat automat de MediClinic. Confidențial — uz medical intern.")
                .SetFont(fontNormal).SetFontSize(8).SetFontColor(colorGray)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Close();
            return stream.ToArray();
        }

        // ---- Helpers ----
        private static Cell Cell9(string text, PdfFont font) =>
            new Cell().Add(new Paragraph(text).SetFont(font).SetFontSize(9)).SetPadding(3);

        private static void AddRow(Table table, string label, string value, PdfFont fontLabel, PdfFont fontValue)
        {
            table.AddCell(new Cell()
                .Add(new Paragraph(label).SetFont(fontLabel).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
            table.AddCell(new Cell()
                .Add(new Paragraph(value).SetFont(fontValue).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
        }
    }
}
