using System;
using System.Linq;
using System.Web.Mvc;
using Fornet.Models;

namespace Fornet.Controllers
{
    public class LoginController : Controller
    {
        FornetDBEntities entity = new FornetDBEntities();

        /*
        // SHA256 hash fonksiyonu (şu an kullanılmıyor)
        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2")); // Hex format
                }
                return builder.ToString();
            }
        }
        */

        // GET : Login
        public ActionResult Index()
        {
            ViewBag.mesaj = null;
            return View();
        }

        // POST : Login
        [HttpPost]
        public ActionResult Index(string kullaniciAd, string parola)
        {
            // SHA256 kapatıldı, şifre düz metin kontrol edilecek
            // string hashedPassword = ComputeSha256Hash(parola);
            string hashedPassword = parola;

            var personel = entity.Personeller
                            .FirstOrDefault(p => p.personelKullaniciAd == kullaniciAd && p.personelParola == hashedPassword);

            if (personel != null)
            {
                Session["PersonelAdSoyad"] = personel.personelAdSoyad;
                Session["PersonelId"] = personel.personelId;
                Session["PersonelBirimId"] = personel.personelBirimId;
                Session["PersonelYetkiTurId"] = personel.personelYetkiTurId;
                Session["PersonelKullaniciAd"] = personel.personelKullaniciAd;

                switch (personel.personelYetkiTurId)
                {
                    case 1:
                        return RedirectToAction("Index", "Dashboard");
                    case 2:
                        return RedirectToAction("Index", "Yonetici");
                    default:
                        ViewBag.mesaj = "Yetkiniz tanımlanmamış.";
                        return View();
                }
            }
            else
            {
                ViewBag.mesaj = "Kullanıcı adı ya da parola yanlış";
                return View();
            }
        }
    }
}
