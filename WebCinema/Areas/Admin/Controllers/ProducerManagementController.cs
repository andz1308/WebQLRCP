using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Admin")]
    public class ProducerManagementController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/ProducerManagement
        public ActionResult Index(string searchTerm)
        {
            var producers = db.Nha_San_Xuats.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                producers = producers.Where(p => p.ten_nha_san_xuat.Contains(searchTerm));
            }

            var result = producers.OrderBy(p => p.ten_nha_san_xuat).ToList();
            ViewBag.SearchTerm = searchTerm;
            return View(result);
        }

        // GET: Admin/ProducerManagement/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/ProducerManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Nha_San_Xuat producer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Nha_San_Xuats.InsertOnSubmit(producer);
                    db.SubmitChanges();

                    TempData["SuccessMessage"] = "Thêm nhà s?n xu?t thành công!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có l?i x?y ra: " + ex.Message;
            }

            return View(producer);
        }

        // GET: Admin/ProducerManagement/Edit/5
        public ActionResult Edit(int id)
        {
            var producer = db.Nha_San_Xuats.FirstOrDefault(p => p.nha_san_xuat_id == id);
            if (producer == null)
            {
                return HttpNotFound();
            }
            return View(producer);
        }

        // POST: Admin/ProducerManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Nha_San_Xuat producer)
        {
            try
            {
                var existingProducer = db.Nha_San_Xuats.FirstOrDefault(p => p.nha_san_xuat_id == id);
                if (existingProducer == null)
                {
                    return HttpNotFound();
                }

                existingProducer.ten_nha_san_xuat = producer.ten_nha_san_xuat;
                existingProducer.quoc_gia = producer.quoc_gia;

                db.SubmitChanges();

                TempData["SuccessMessage"] = "C?p nh?t nhà s?n xu?t thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có l?i x?y ra: " + ex.Message;
            }

            return View(producer);
        }

        // POST: Admin/ProducerManagement/Delete/5
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                var producer = db.Nha_San_Xuats.FirstOrDefault(p => p.nha_san_xuat_id == id);
                if (producer == null)
                {
                    return Json(new { success = false, message = "Nhà s?n xu?t không t?n t?i." });
                }

                // Check if producer has movies
                if (producer.Phims.Any())
                {
                    return Json(new { success = false, message = "Không th? xóa nhà s?n xu?t ?ã có phim." });
                }

                db.Nha_San_Xuats.DeleteOnSubmit(producer);
                db.SubmitChanges();

                return Json(new { success = true, message = "Xóa nhà s?n xu?t thành công!" });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = "Có l?i x?y ra: " + ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}