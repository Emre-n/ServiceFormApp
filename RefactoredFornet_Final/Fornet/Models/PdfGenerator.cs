using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.IO;
using System.Linq;
using System.Web;
using Fornet.Models;

public static class PdfGenerator
{
    public static byte[] GeneratePdf(ServiceForms form)
    {
        using (var context = new FornetDBEntities())
        {
            string fontPath = HttpContext.Current.Server.MapPath("~/Fonts/arial.ttf");
            BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            Font font = new Font(baseFont, 11);
            Font boldFont = new Font(baseFont, 11, Font.BOLD);

            var storeName = context.Magazalar.FirstOrDefault(m => m.StoreId.ToString() == form.StoreSelect)?.StoreName;
            var categoryName = context.Category.FirstOrDefault(c => c.categoryId == form.CategoryId)?.categoryIsmi;
            var subcategoryName = context.Subcategory.FirstOrDefault(s => s.subcategoryId == form.SubcategoryId)?.subcategoryIsmi;
            var siteName = context.Markalar.FirstOrDefault(m => m.markaId.ToString() == form.Site)?.markaIsım;

            using (var ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 50, 50, 50, 80);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                // Logo
                string logoPath = HttpContext.Current.Server.MapPath("~/Files/logo.png");
                if (File.Exists(logoPath))
                {
                    var logo = Image.GetInstance(logoPath);
                    logo.ScaleToFit(120f, 120f);
                    logo.Alignment = Element.ALIGN_CENTER;
                    document.Add(logo);
                }

                // Bilgiler tablosu
                PdfPTable table = new PdfPTable(2);
                table.WidthPercentage = 100;
                table.SpacingBefore = 10f;
                table.SpacingAfter = 20f;

                void AddCell(string label, string value)
                {
                    table.AddCell(new PdfPCell(new Phrase(label, boldFont)) { Border = Rectangle.BOX, Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(value ?? "-", font)) { Border = Rectangle.BOX, Padding = 5 });
                }

                AddCell("Helpdesk ID", form.HelpdeskId);
                AddCell("INC Number", form.IncNumber);
                AddCell("Service Type", form.ServiceType);
                AddCell("Site", siteName);
                AddCell("Store", storeName);
                AddCell("Mağaza Adresi", form.Adress);
                AddCell("İl / İlçe", form.CityDistrict);
                AddCell("Category", categoryName);
                AddCell("Subcategory", subcategoryName);
                AddCell("Vendor SR", form.VendorSR);

                var itemIds = form.Items?.Split(',') ?? new string[] { };
                var itemNames = context.Items
                    .Where(i => itemIds.Contains(i.itemId.ToString()))
                    .Select(i => i.itemName)
                    .ToList();
                AddCell("Items", string.Join(", ", itemNames));

                AddCell("Arrival DateTime", form.ArrivalDateTime?.ToString("dd.MM.yyyy HH:mm"));
                AddCell("Departure DateTime", form.DepartureDateTime?.ToString("dd.MM.yyyy HH:mm"));
                AddCell("Form Details", form.FormDetails);

                document.Add(table);

                PdfContentByte cb = writer.DirectContent;
                float imzaY = 120f;
                float imzaX1 = 60f;
                float imzaX2 = 350f;

                // İsimler üstte
                ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT, new Phrase("Technician: " + form.Technician, boldFont), imzaX1, imzaY + 80f, 0);
                ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT, new Phrase("Support Person: " + form.SupportPersonName, boldFont), imzaX2, imzaY + 80f, 0);

                // İmza başlıkları
                ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT, new Phrase("Technician İmzası", font), imzaX1, imzaY + 60f, 0);
                ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT, new Phrase("Support Person İmzası", font), imzaX2, imzaY + 60f, 0);

                // Technician PNG imzası
                if (!string.IsNullOrEmpty(form.TechnicianSignatureFileName))
                {
                    string pngPath = HttpContext.Current.Server.MapPath("~/Files/" + form.TechnicianSignatureFileName);
                    if (File.Exists(pngPath))
                    {
                        var pngSig = Image.GetInstance(pngPath);
                        pngSig.ScaleAbsolute(150f, 70f);
                        pngSig.SetAbsolutePosition(imzaX1, imzaY);
                        cb.AddImage(pngSig);
                    }
                }

                // Support çizim imzası (base64)
                if (!string.IsNullOrEmpty(form.SignatureData))
                {
                    try
                    {
                        var base64Data = form.SignatureData.Split(',')[1];
                        byte[] imageBytes = Convert.FromBase64String(base64Data);
                        var sig = Image.GetInstance(imageBytes);
                        sig.ScaleAbsolute(150f, 70f);
                        sig.SetAbsolutePosition(imzaX2, imzaY);
                        cb.AddImage(sig);
                    }
                    catch (Exception ex)
                    {
                        document.Add(new Paragraph("İmza yüklenemedi: " + ex.Message, font));
                    }
                }

                document.Close();
                return ms.ToArray();
            }
        }
    }
}
