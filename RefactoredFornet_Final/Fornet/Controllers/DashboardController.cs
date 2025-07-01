using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Fornet.Models;

namespace Fornet.Controllers
{
    public class DashboardController : Controller
    {
        FornetDBEntities entity = new FornetDBEntities();

        public ActionResult Index()
        {
            if (Session["PersonelYetkiTurId"] == null || Session["PersonelKullaniciAd"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            int yetkiTurId = Convert.ToInt32(Session["PersonelYetkiTurId"]);
            if (yetkiTurId != 1)
            {
                return RedirectToAction("Index", "Login");
            }

            string kullaniciAd = Session["PersonelKullaniciAd"].ToString();
            var personel = entity.Personeller.FirstOrDefault(p => p.personelKullaniciAd == kullaniciAd);

            if (personel == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index", "Login");
            }

            ViewBag.AdSoyad = personel.personelAdSoyad;
            ViewBag.Yetki = yetkiTurId;

            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);

            var bugunkuFormSayisi = entity.ServiceForms
                .Count(f => f.Technician == personel.personelAdSoyad &&
                            f.ArrivalDateTime >= bugun &&
                            f.ArrivalDateTime < yarin);

            var toplamFormSayisi = entity.ServiceForms
                .Count(f => f.Technician == personel.personelAdSoyad);

            var sonFormlar = entity.ServiceForms
                .OrderByDescending(f => f.ArrivalDateTime)
                .Take(5)
                .ToList();

            ViewBag.BugunkuFormSayisi = bugunkuFormSayisi;
            ViewBag.ToplamFormSayisi = toplamFormSayisi;
            ViewBag.SonFormlar = sonFormlar;

            return View();
        }

        public JsonResult GetWeeklyFormData()
        {
            if (Session["PersonelKullaniciAd"] == null)
                return Json(new { success = false, message = "Oturum yok" }, JsonRequestBehavior.AllowGet);

            string kullaniciAd = Session["PersonelKullaniciAd"].ToString();
            var personel = entity.Personeller.FirstOrDefault(p => p.personelKullaniciAd == kullaniciAd);

            if (personel == null)
                return Json(new { success = false, message = "Kullanıcı yok" }, JsonRequestBehavior.AllowGet);

            var today = DateTime.Today;
            var haftalikVeri = new List<object>();

            for (int i = 6; i >= 0; i--)
            {
                var gun = today.AddDays(-i);
                var ertesiGun = gun.AddDays(1);

                var sayi = entity.ServiceForms
                    .Count(f => f.Technician == personel.personelAdSoyad &&
                                f.ArrivalDateTime >= gun && f.ArrivalDateTime < ertesiGun);

                haftalikVeri.Add(new { tarih = gun.ToString("dd.MM"), adet = sayi });
            }

            return Json(haftalikVeri, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetServiceTypeStats()
        {
            var stats = entity.ServiceForms
                .GroupBy(f => f.ServiceType)
                .Select(g => new { label = g.Key, count = g.Count() })
                .ToList();

            return Json(stats, JsonRequestBehavior.AllowGet);
        }
    }
}
