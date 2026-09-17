using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Empire_ERP.Core.Entities;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Renderer;
using ZXing;
using ZXing.Common;
using ZXing.OneD;
using ZXing.QrCode;
using Cell = iText.Layout.Element.Cell;
using Table = iText.Layout.Element.Table;

namespace Empire_ERP.Core.Services
{
    public class PdfService
    {
        public static void GenerateSlipPdf(SlipViewModel slip, string filePath, string webRootPath)
        {
            if (slip.JobName != "KOT")
            {
                return;
            }

            if (slip == null)
                throw new ArgumentNullException(nameof(slip));

            try
            {
                // Ensure folder exists
                var folder = System.IO.Path.GetDirectoryName(filePath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                float thermalWidth = 204.9f; // 80mm in points
                float thermalHeight = 800f; // Dynamic height, will adjust

                // Create PDF writer and document
                using var writer = new PdfWriter(filePath);
                using var pdf = new PdfDocument(writer);
                var pageSize = new PageSize(thermalWidth, thermalHeight);
                var doc = new iText.Layout.Document(pdf, pageSize);

                // Minimal margins for thermal printer
                doc.SetMargins(5, 5, 5, 5);

                var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Add logo if exists
                if (!string.IsNullOrEmpty(slip.Clogo))
                {
                    string logoPath = System.IO.Path.Combine(webRootPath, "Client", "Company", slip.Clogo);

                    if (File.Exists(logoPath))
                    {
                        var imgData = ImageDataFactory.Create(logoPath);
                        var logo = new iText.Layout.Element.Image(imgData);
                        logo.ScaleToFit(140, 70);
                        logo.SetHorizontalAlignment(HorizontalAlignment.CENTER);
                        doc.Add(logo);
                    }
                }

                // Generate and add QR Code
                if (!string.IsNullOrEmpty(slip.ReferenceNumber) && slip.QRCode == 1)
                {
                    byte[] qrCodeBytes = GenerateQRCodeBytes(slip.ReferenceNumber);
                    if (qrCodeBytes != null && qrCodeBytes.Length > 0)
                    {
                        var qrCodeImage = ImageDataFactory.Create(qrCodeBytes);
                        var qrImg = new iText.Layout.Element.Image(qrCodeImage);
                        qrImg.ScaleToFit(80, 80);
                        qrImg.SetHorizontalAlignment(HorizontalAlignment.CENTER);
                        doc.Add(qrImg);
                    }
                }

                // Reference Number - Left aligned
                var refPara = new Paragraph()
                    .Add(new Text("REF #: ").SetFont(boldFont).SetFontSize(10))
                    .Add(new Text(slip.ReferenceNumber ?? "").SetFont(normalFont).SetFontSize(10)).SetMarginBottom(0);
                doc.Add(refPara);

                // Date and Time - Left aligned
                var datePara = new Paragraph()
                    .Add(new Text("DATE: ").SetFont(boldFont).SetFontSize(10))
                    .Add(new Text($"{slip.Date ?? ""} {slip.Time ?? ""}").SetFont(normalFont).SetFontSize(10)).SetMarginTop(0).SetMarginBottom(2);
                doc.Add(datePara);

                // Waiter (if available) - Left aligned
                if (!string.IsNullOrEmpty(slip.Waitername))
                {
                    var waiterPara = new Paragraph()
                        .Add(new Text("Waiter: ").SetFont(boldFont).SetFontSize(10))
                        .Add(new Text(slip.Waitername).SetFont(normalFont).SetFontSize(10)).SetMarginTop(0).SetMarginBottom(2);
                    doc.Add(waiterPara);
                }

                // Table (if available) - Left aligned
                if (!string.IsNullOrEmpty(slip.TableNum))
                {
                    var tablePara = new Paragraph()
                        .Add(new Text("Table: ").SetFont(boldFont).SetFontSize(10))
                        .Add(new Text(slip.TableNum).SetFont(normalFont).SetFontSize(10)).SetMarginTop(0).SetMarginBottom(2);
                    doc.Add(tablePara);
                }
                // KOT RECEIPT Header with border - Centered
                var kotHeaderDiv = new Div()
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetPadding(0)
                    .SetBorder(new iText.Layout.Borders.SolidBorder(iText.Kernel.Colors.ColorConstants.BLACK, 1));
                kotHeaderDiv.Add(new Paragraph("KOT RECEIPT")
                    .SetFont(boldFont)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMargin(0));
                doc.Add(kotHeaderDiv);

                // Items Table
                Table table = new Table(UnitValue.CreatePercentArray(new float[] { 75, 25 }))
                .UseAllAvailableWidth()
                .SetFontSize(9)
                .SetMarginTop(5)
                .SetMarginBottom(5);


                var descHeader = new Cell()
                .Add(new Paragraph("Description").SetFont(boldFont).SetFontSize(9))
                .SetTextAlignment(TextAlignment.LEFT)
                .SetPadding(2)
                .SetBorder(new SolidBorder(ColorConstants.BLACK, 0.7f));
                table.AddHeaderCell(descHeader);

                var qtyHeader = new Cell()
                    .Add(new Paragraph("Qty").SetFont(boldFont).SetFontSize(9))
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetPadding(2)
                    .SetBorder(new SolidBorder(ColorConstants.BLACK, 0.7f));
                table.AddHeaderCell(qtyHeader);

                if (slip.Items != null)
                {
                    for (int i = 0; i < slip.Items.Count; i++)
                    {
                        var item = slip.Items[i];
                        bool isLastRow = (i == slip.Items.Count - 1);

                        float borderWidth = isLastRow ? 1.5f : 0.5f;

                        // Description
                        var descCell = new Cell()
                            .Add(new Paragraph(item.Description ?? "")
                            .SetFont(normalFont)
                            .SetFontSize(9)
                            .SetMargin(0))
                            .SetPadding(2)
                            .SetBorderTop(Border.NO_BORDER)
                            .SetBorderLeft(Border.NO_BORDER)
                            .SetBorderRight(Border.NO_BORDER)
                            .SetBorderBottom(new SolidBorder(ColorConstants.BLACK, borderWidth));

                        table.AddCell(descCell);

                        // Qty
                        var qtyCell = new Cell()
                            .Add(new Paragraph(item.Quantity?.ToString() ?? "0")
                            .SetFont(normalFont)
                            .SetFontSize(9)
                            .SetMargin(0))
                            .SetTextAlignment(TextAlignment.RIGHT)
                            .SetPadding(2)
                            .SetBorderTop(Border.NO_BORDER)
                            .SetBorderLeft(Border.NO_BORDER)
                            .SetBorderRight(Border.NO_BORDER)
                            .SetBorderBottom(new SolidBorder(ColorConstants.BLACK, borderWidth));

                        table.AddCell(qtyCell);
                    }
                }
                doc.Add(table);
                // Counter/Salesman - Left aligned
                var counterPara = new Paragraph()
                    .Add(new Text("Counter: ").SetFont(boldFont).SetFontSize(10))
                    .Add(new Text(slip.SalesmanName ?? "").SetFont(normalFont).SetFontSize(10)).SetMarginTop(0).SetMarginBottom(2);
                doc.Add(counterPara);

                doc.Close();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error generating PDF slip", ex);
            }
        }
        private static byte[] GenerateQRCodeBytes(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return null;

                text = text.Trim();

                // Define QR code encoding options
                var options = new EncodingOptions
                {
                    Height = 300,
                    Width = 300,
                    Margin = 1
                };

                // Create a barcode writer
                var barcodeWriter = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = options
                };

                // Generate QR code pixel data
                var pixelData = barcodeWriter.Write(text);

                // Convert pixel data to a PNG image
                using (var ms = new MemoryStream())
                {
                    using (var bitmap = new System.Drawing.Bitmap(pixelData.Width, pixelData.Height))
                    {
                        for (int y = 0; y < pixelData.Height; y++)
                        {
                            for (int x = 0; x < pixelData.Width; x++)
                            {
                                var color = pixelData.Pixels[(y * pixelData.Width + x) * 4];
                                bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(color, color, color));
                            }
                        }

                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    }

                    return ms.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }
        public static string GenerateStickers(SlipViewModel slip, string filePath, string webRootPath)
        {
            if(slip.JobName != "STICKER")
            {
                return string.Empty;
            }
            List<string> stickerPaths = new List<string>();
            if (slip == null)
                throw new ArgumentNullException(nameof(slip));

            try
            {
                // Ensure folder exists
                var folder = System.IO.Path.GetDirectoryName(filePath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                // Convert cm to points: 1 cm = 28.35 points
                // 4cm height = 113.4 points, 5cm width = 141.75 points
                float stickerWidth = 127.575f;  // 5cm
                float stickerHeight = 108.4f;  // 4cm

                string fontRegularPath = System.IO.Path.Combine(webRootPath, "fonts", "TCM_____.TTF");
                string fontBoldPath = System.IO.Path.Combine(webRootPath, "fonts", "TCB_____.TTF");

                
                foreach (var item in slip.Items)
                {
                    if (item.Description == "# Of Items :")
                        break;

                    int totalQty = (int)(item.Quantity ?? 0);
                    string safeDesc = (item.Description ?? "Item").Replace("/", "_").Replace("\\", "_");
                    string stickerPath = System.IO.Path.Combine(
                                            folder,
                                            $"Sticker_{safeDesc}_Qty_{totalQty}.pdf"
                                        );

                    using (PdfWriter writer = new PdfWriter(stickerPath))
                    {
                        PdfDocument pdf = new PdfDocument(writer);

                        PdfFont normalFont = PdfFontFactory.CreateFont(
                            fontRegularPath,
                            PdfEncodings.IDENTITY_H,
                            PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED
                        );

                        PdfFont boldFont = PdfFontFactory.CreateFont(
                            fontBoldPath,
                            PdfEncodings.IDENTITY_H,
                            PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED
                        );

                        using (iText.Layout.Document doc =
                               new iText.Layout.Document(pdf, new PageSize(stickerWidth, stickerHeight)))
                        {
                            doc.SetMargins(1, 2, 2, 10);

                            // Top section with logo and quantity
                            var topTable = new Table(new float[] { 25, 50, 25 })
                                .UseAllAvailableWidth()
                                .SetMarginBottom(2);

                            // Left cell - Empty space for circle (already drawn)
                            var leftCell = new Cell()
                                .SetBorder(Border.NO_BORDER)
                                .SetPadding(0)
                                .SetHeight(16);
                            leftCell.Add(new Paragraph(" ").SetMargin(0).SetPadding(0));

                            // Center cell - Logo (small) + Dine In
                            var centerCell = new Cell()
                                .SetBorder(Border.NO_BORDER)
                                .SetPadding(0)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetVerticalAlignment(VerticalAlignment.MIDDLE);

                            // Add logo if exists (responsive size based on sticker dimensions)
                            float logoMaxWidth = stickerWidth * 0.999f;
                            float logoMaxHeight = stickerHeight * 0.250f;

                            if (!string.IsNullOrEmpty(slip.StickerLogo))
                            {
                                string logoPath = null;
                                if (!string.IsNullOrEmpty(slip.StickerLogo))
                                {
                                    var relativeLogo = slip.StickerLogo.TrimStart('/', '\\');
                                    logoPath = System.IO.Path.Combine(webRootPath, relativeLogo);
                                }
                                if (File.Exists(logoPath))
                                {
                                    var imgData = ImageDataFactory.Create(logoPath);
                                    var logo = new iText.Layout.Element.Image(imgData);
                                    logo.ScaleToFit(logoMaxWidth, logoMaxHeight);
                                    logo.SetHorizontalAlignment(HorizontalAlignment.CENTER);
                                    centerCell.Add(logo);
                                }
                            }

                            centerCell.Add(new Paragraph(slip.Type ?? "Status")
                                .SetFont(boldFont)
                                .SetFontSize(8)
                                .SetFontColor(ColorConstants.BLACK)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginTop(0)
                                .SetMarginBottom(0));


                            topTable.AddCell(leftCell);
                            topTable.AddCell(centerCell);
                            doc.Add(topTable);

                            var itemTable = new Table(1)
                            .UseAllAvailableWidth()
                            .SetMarginBottom(1);

                            // Calculate circle size based on sticker dimensions
                            float circleSize = Math.Min(stickerWidth, stickerHeight) * 0.10f;

                            var circleCell = new Cell()
                                .SetBorder(Border.NO_BORDER)
                                .SetHeight(circleSize)
                                .SetPadding(0)
                                .SetMarginBottom(2);

                            circleCell.SetNextRenderer(new CircleTextCellRenderer(circleCell, $"{item.ItemCode}", boldFont, 7));

                            itemTable.AddCell(circleCell);

                            itemTable.AddCell(new Cell()
                                .Add(new Paragraph(item.Description ?? "")
                                    .SetFont(boldFont)
                                    .SetFontSize(8)
                                    .SetFontColor(ColorConstants.BLACK))
                                .SetBorder(Border.NO_BORDER)
                                .SetPadding(0)
                                .SetMarginBottom(0).SetMarginTop(0));

                            doc.Add(itemTable);

                            // Date/Time and Reference Code Row
                            var infoRow = new Table(new float[] { 1, 1 })
                                .UseAllAvailableWidth()
                                .SetMarginBottom(0).SetMarginTop(0);

                            // Format date/time to match image format (MM/dd/yyyy hh:mm tt)
                            string dateTime = $"{slip.Date ?? ""} {slip.Time ?? ""}";
                            if (!string.IsNullOrEmpty(slip.Date) && DateTime.TryParse(slip.Date, out DateTime parsedDate))
                            {
                                string timePart = slip.Time ?? "";
                                DateTime dateTimeObj = parsedDate;

                                if (!string.IsNullOrEmpty(timePart))
                                {
                                    if (TimeSpan.TryParse(timePart, out TimeSpan parsedTime))
                                    {
                                        dateTimeObj = parsedDate.Date.Add(parsedTime);
                                    }
                                    else if (DateTime.TryParse($"{slip.Date} {slip.Time}", out DateTime fullDateTime))
                                    {
                                        dateTimeObj = fullDateTime;
                                    }
                                }

                                dateTime = dateTimeObj.ToString("MM/dd/yyyy hh:mm tt");
                            }

                            infoRow.AddCell(new Cell()
                                .Add(new Paragraph(dateTime)
                                    .SetFont(normalFont)
                                    .SetFontSize(7))
                                .SetFontColor(ColorConstants.BLACK)
                                .SetBorder(Border.NO_BORDER)
                                .SetPadding(0).SetMarginTop(0)
                                .SetMarginBottom(0));

                            infoRow.AddCell(new Cell()
                                .Add(new Paragraph(slip.LocationShortName)
                                    .SetFont(normalFont)
                                    .SetFontSize(7)
                                    .SetFontColor(ColorConstants.BLACK)
                                    .SetTextAlignment(TextAlignment.RIGHT))
                                .SetBorder(Border.NO_BORDER)
                                .SetPadding(0)
                                .SetTextAlignment(TextAlignment.RIGHT).SetMarginTop(5)
                                .SetMarginBottom(0));

                            doc.Add(infoRow);

                            // Customer Name (centered)
                            doc.Add(new Paragraph(slip.CustomerName ?? "Guest")
                                .SetFont(boldFont)
                                .SetFontSize(8)
                                .SetFontColor(ColorConstants.BLACK)
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetMarginTop(-1)
                                .SetMarginBottom(0));

                            doc.Add(new Paragraph($"Powered by : {slip.FName} ")
                            .SetFont(normalFont)
                            .SetFontSize(7)
                            .SetFontColor(ColorConstants.BLACK)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFixedPosition(
                                0,
                                3,              // bottom se thoda upar
                                stickerWidth    // full width
                            )
                            .SetMargin(0)
                            .SetPadding(0));

                        }
                    }
                    // Add sticker path to the list
                    stickerPaths.Add(stickerPath);

                }

                // Return comma-separated sticker paths
                return string.Join(",", stickerPaths);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error generating Sticker PDFs", ex);
            }
        }

    }
    public class CircleTextCellRenderer : CellRenderer
    {
        private readonly string _text;
        private readonly PdfFont _font;
        private readonly float _fontSize;

        public CircleTextCellRenderer(Cell modelElement, string text, PdfFont font, float fontSize = 6f)
            : base(modelElement)
        {
            _text = text;
            _font = font;
            _fontSize = fontSize;
        }
        public override void Draw(DrawContext drawContext)
        {
            base.Draw(drawContext);

            var canvas = drawContext.GetCanvas();
            var rect = GetOccupiedAreaBBox();

            // Bigger circle
            float available = Math.Min(rect.GetWidth(), rect.GetHeight());
            float radius = available * 0.50f;

            // LEFT alignment
            float cx = rect.GetLeft() + radius + 1;
            float cy = rect.GetBottom() + rect.GetHeight() / 2;

            canvas.SaveState();

            canvas.SetFillColor(ColorConstants.BLACK);
            canvas.Circle(cx, cy, radius);
            canvas.Fill();

            // Text
            canvas.BeginText();
            canvas.SetFontAndSize(_font, _fontSize);
            canvas.SetFillColor(ColorConstants.WHITE);

            float textWidth = _font.GetWidth(_text, _fontSize);
            float ascent = _font.GetAscent(_text, _fontSize);
            float descent = _font.GetDescent(_text, _fontSize);

            float textX = cx - (textWidth / 2);
            float textY = cy - ((ascent + descent) / 2);

            canvas.SetTextMatrix(textX, textY);
            canvas.ShowText(_text);
            canvas.EndText();

            canvas.RestoreState();
        }



    }
}

