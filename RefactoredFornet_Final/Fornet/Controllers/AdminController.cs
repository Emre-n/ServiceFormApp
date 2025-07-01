using Fornet.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc;

namespace Fornet.Controllers
{
    public class AdminController : Controller
    {
        FornetDBEntities entity = new FornetDBEntities();

        // Admin paneli - form listeleme, filtreleme ve sayfalama
        public ActionResult Index(DateTime? startDate, DateTime? endDate, int page = 1)
        {
            // Oturum kontrolü
            if (Session["personelBirimId"] == null || Session["personelAdSoyad"] == null)
                return RedirectToAction("Index", "Login");

            int birimId = Convert.ToInt32(Session["personelBirimId"]);
            string personelAdSoyad = Session["personelAdSoyad"].ToString();

            // Tüm formları sorgula
            var formlarQuery = entity.ServiceForms.AsQueryable();

            // Eğer personelse sadece kendi formlarını görsün
            if (birimId != 1)
            {
                formlarQuery = formlarQuery.Where(f => f.Technician == personelAdSoyad);
            }

            // Tarih filtrelemesi yapılmışsa uygula
            if (startDate.HasValue && endDate.HasValue)
            {
                DateTime endDateAdjusted = endDate.Value.AddDays(1);
                formlarQuery = formlarQuery.Where(f => f.ArrivalDateTime >= startDate.Value && f.ArrivalDateTime < endDateAdjusted);
            }
            else
            {
                // Eğer tarih girilmemişse bugünün formlarını getir
                DateTime bugun = DateTime.Today;
                DateTime yarin = bugun.AddDays(1);
                formlarQuery = formlarQuery.Where(f => f.ArrivalDateTime >= bugun && f.ArrivalDateTime < yarin);
            }

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            // Sayfalama uygula
            var pagedFormlar = formlarQuery
                .OrderByDescending(f => f.ArrivalDateTime)
                .ToPagedList(page, 15);

            return View(pagedFormlar);
        }

        public ActionResult IndirPdf(int id)
        {
            var form = entity.ServiceForms.FirstOrDefault(x => x.Id == id);
            if (form == null)
                return HttpNotFound();

            byte[] pdfBytes = PdfGenerator.GeneratePdf(form);
            return File(pdfBytes, "application/pdf", $"ServisFormu_{id}.pdf");
        }

        [HttpPost]
        public ActionResult Onayla(int id)
        {
            var form = entity.ServiceForms.Find(id);
            if (form != null)
            {
                form.Durum = 1;
                entity.SaveChanges();

                // PDF oluştur
                byte[] pdfBytes = PdfGenerator.GeneratePdf(form);
                string pdfPath = Server.MapPath($"~/Temp/ServisFormu_{id}.pdf");
                System.IO.File.WriteAllBytes(pdfPath, pdfBytes);

                // Mağaza email adresini al
                var magazEmail = entity.Magazalar
                    .Where(m => m.StoreId.ToString() == form.StoreSelect)
                    .Select(m => m.Email)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(magazEmail))
                {
                    try
                    {
                        var mail = new MailMessage();
                        mail.From = new MailAddress("formfornet@gmail.com", "Teknnonet Destek");
                        mail.To.Add(magazEmail);
                        mail.Subject = "Servis Formu Bilgilendirmesi";
                        mail.Body = "Merhaba,\n\nİlgili servis formunuz ekte PDF olarak iletilmiştir.\n\nİyi çalışmalar.";
                        mail.Attachments.Add(new Attachment(pdfPath));

                        var smtp = new SmtpClient("smtp.gmail.com", 587)
                        {
                            Credentials = new NetworkCredential("formfornet@gmail.com", "zdbn qfmx imke leng"),
                            EnableSsl = true
                        };

                        smtp.Send(mail);
                        TempData["SuccessMessage"] = "Form başarıyla onaylandı ve e-posta gönderildi.";
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = "Mail gönderimi sırasında hata oluştu: " + ex.Message;
                    }
                }
                else
                {
                    TempData["Error"] = $"'{form.Site}' mağazasına ait e-posta adresi tanımlı değil.";
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Reddet(int id)
        {
            var form = entity.ServiceForms.Find(id);
            if (form != null)
            {
                form.Durum = 2;
                entity.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Sil(int id)
        {
            var form = entity.ServiceForms.Find(id);
            if (form != null)
            {
                entity.ServiceForms.Remove(form);
                entity.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult FormDetay(int id)
        {
            var servisForm = entity.ServiceForms.FirstOrDefault(s => s.Id == id);
            if (servisForm == null)
                return HttpNotFound("Servis formu bulunamadı.");

            return View(servisForm);
        }

        public ActionResult FormGuncelle(int id)
        {
            var form = entity.ServiceForms.FirstOrDefault(f => f.Id == id);
            if (form == null)
                return HttpNotFound();

            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FormGuncelle(ServiceForms updatedForm)
        {
            var existingForm = entity.ServiceForms.FirstOrDefault(f => f.Id == updatedForm.Id);
            if (existingForm == null)
                return HttpNotFound();

            //Alanları güncelle
            existingForm.HelpdeskId = updatedForm.HelpdeskId;
            existingForm.IncNumber = updatedForm.IncNumber;
            existingForm.ServiceType = updatedForm.ServiceType;
            existingForm.Site = updatedForm.Site;
            existingForm.StoreSelect = updatedForm.StoreSelect;
            existingForm.CategoryId = updatedForm.CategoryId;
            existingForm.SubcategoryId = updatedForm.SubcategoryId;
            existingForm.Items = updatedForm.Items;
            existingForm.VendorSR = updatedForm.VendorSR;
            existingForm.ArrivalDateTime = updatedForm.ArrivalDateTime;
            existingForm.DepartureDateTime = updatedForm.DepartureDateTime;
            existingForm.Technician = updatedForm.Technician;
            existingForm.SupportPersonName = updatedForm.SupportPersonName;
            existingForm.FormDetails = updatedForm.FormDetails;
            existingForm.SignatureData = updatedForm.SignatureData;

            entity.SaveChanges();

            TempData["SuccessMessage"] = "Form başarıyla güncellendi.";
            return RedirectToAction("Index");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Login");
        }
    }
}
