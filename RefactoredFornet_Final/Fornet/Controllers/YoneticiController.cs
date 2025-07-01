using System;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security.Policy;
using System.Web.Mvc;
using Fornet.Models;

namespace Fornet.Controllers
{
    public class YoneticiController : Controller
    {
        FornetDBEntities entity = new FornetDBEntities();

        // GET: Yonetici
        public ActionResult Index()
        {
            int yetkiTurId = Convert.ToInt32(Session["PersonelYetkiTurId"]);
            if (yetkiTurId == 1)
            {
                string kullaniciAd = Session["PersonelKullaniciAd"]?.ToString();
                var personel = entity.Personeller.FirstOrDefault(p => p.personelKullaniciAd == kullaniciAd);
                ViewBag.PersonelAdSoyad = personel?.personelAdSoyad;

                if (personel?.personelImza != null)
                {
                    ViewBag.PersonelImza = Convert.ToBase64String(personel.personelImza);
                }

                var category = entity.Category.ToList();
                var items = entity.Items.ToList();
                var stores = entity.Magazalar
                    .Select(m => new SelectListItem
                    {
                        Value = m.StoreId.ToString(),
                        Text = m.StoreCode + " " + m.StoreName
                    })
                    .ToList();

                var markalar = entity.Markalar
                    .Select(m => new SelectListItem
                    {
                        Value = m.markaId.ToString(),
                        Text = m.markaIsım
                    })
                    .ToList();

                ViewBag.Category = new SelectList(category, "CategoryId", "CategoryIsmi");
                ViewBag.Items = new SelectList(items, "ItemId", "ItemName");
                ViewBag.Stores = stores;
                ViewBag.Markalar = markalar;

                return View();
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }

        public JsonResult GetSubcategories(int categoryId)
        {
            var categorySet = entity.Category
                .Where(c => c.categoryId == categoryId)
                .Select(c => c.categorySet)
                .FirstOrDefault();

            var subcategories = entity.Subcategory
                .Where(s => s.subcategorySet == categorySet)
                .Select(s => new SelectListItem
                {
                    Value = s.subcategoryId.ToString(),
                    Text = s.subcategoryIsmi
                })
                .ToList();

            return Json(subcategories, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetItems(int subcategoryId)
        {
            var items = entity.Items
                .Where(i => i.itemSet == subcategoryId)
                .Select(i => new SelectListItem
                {
                    Value = i.itemId.ToString(),
                    Text = i.itemName
                }).ToList();

            return Json(items, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SearchStores(string term)
        {
            var results = entity.Magazalar
                .Where(m => m.StoreCode.ToString().Contains(term) || m.StoreName.Contains(term))
                .Select(m => new
                {
                    id = m.StoreId,
                    text = m.StoreCode + " " + m.StoreName
                })
                .ToList();

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Login");
        }
        [HttpGet]
        public JsonResult GetStoreDetails(int storeId)
        {
            var store = entity.Magazalar
                .Where(m => m.StoreId == storeId)
                .Select(m => new
                {
                    Adress = m.Adress,
                    City = m.City,
                    District = m.District
                })
                .FirstOrDefault();

            return Json(store, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GeneratePdf(FormCollection form)
        {
            try
            {
                var HelpdeskId = form["HelpdeskId"];
                var incNumber = form["incNumber"];
                var serviceType = form["serviceType"];
                //var site = entity.Magazalar.FirstOrDefault(m => m.StoreId.ToString() == form["site"]).StoreName;
                var site = form["site"];
                var storeSelect = form["storeSelect"];
                var category = form["Category"];
                var subcategory = form["Subcategory"];
                var itemValues = form.GetValues("Item[]");
                var vendorSR = form["vendorSR"];
                var arrivalDateTime = form["arrivalDateTime"];
                var departureDateTime = form["departureDateTime"];
                var technician = form["technician"];
                var formdetails = form["formdetails"];
                var signatureData = form["signatureData"];
                var supportPersonName = form["supportPersonName"];
                var technicianSignatureFile = form["technicianSignatureFile"]; // Yeni alan
                var storeAddress = form["Adress"];
                var cityDistrict = form["CityDistrict"];

                if (itemValues == null || itemValues.Length == 0)
                {
                    return Content("Ürün seçilmedi!");
                }



                string connectionString = "Server=DESKTOP-5I671TM\\SQLEXPRESS;Database=FornetDB;Integrated Security=True;";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"INSERT INTO ServiceForms 
                        (HelpdeskId, IncNumber, ServiceType, Site, StoreSelect, CategoryId, SubcategoryId, Items, VendorSR, ArrivalDateTime, DepartureDateTime, Technician, SupportPersonName, SignatureData, FormDetails, TechnicianSignatureFileName, Adress, CityDistrict)
                        VALUES 
                        (@HelpdeskId, @IncNumber, @ServiceType, @Site, @StoreSelect, @CategoryId, @SubcategoryId, @Items, @VendorSR, @ArrivalDateTime, @DepartureDateTime, @Technician, @SupportPersonName, @SignatureData, @FormDetails, @TechnicianSignatureFileName, @Adress, @CityDistrict)";


                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@HelpdeskId", HelpdeskId);
                        cmd.Parameters.AddWithValue("@IncNumber", incNumber);
                        cmd.Parameters.AddWithValue("@ServiceType", serviceType);
                        cmd.Parameters.AddWithValue("@Site", site); 
                        cmd.Parameters.AddWithValue("@StoreSelect", storeSelect);
                        cmd.Parameters.AddWithValue("@CategoryId", category);
                        cmd.Parameters.AddWithValue("@SubcategoryId", subcategory);
                        cmd.Parameters.AddWithValue("@Items", string.Join(",", itemValues));
                        cmd.Parameters.AddWithValue("@VendorSR", vendorSR);

                        if (DateTime.TryParse(arrivalDateTime, out DateTime arrival))
                            cmd.Parameters.AddWithValue("@ArrivalDateTime", arrival);
                        else
                            cmd.Parameters.AddWithValue("@ArrivalDateTime", DBNull.Value);

                        if (DateTime.TryParse(departureDateTime, out DateTime departure))
                            cmd.Parameters.AddWithValue("@DepartureDateTime", departure);
                        else
                            cmd.Parameters.AddWithValue("@DepartureDateTime", DBNull.Value);

                        cmd.Parameters.AddWithValue("@Technician", technician);
                        cmd.Parameters.AddWithValue("@SupportPersonName", supportPersonName);
                        cmd.Parameters.AddWithValue("@SignatureData", signatureData);
                        cmd.Parameters.AddWithValue("@FormDetails", formdetails);
                        cmd.Parameters.AddWithValue("@TechnicianSignatureFileName", technicianSignatureFile); // Yeni parametre
                        cmd.Parameters.AddWithValue("@Adress", storeAddress);
                        cmd.Parameters.AddWithValue("@CityDistrict", cityDistrict);

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Servis formu başarıyla kaydedildi.";
                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                return Content("Hata: " + ex.Message);
            }
        }
    }
}
