using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Admin")]
    public class DirectorManagementController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/DirectorManagement
        public ActionResult Index(string searchTerm)
        {
            var directors = db.Dao_Diens.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                directors = directors.Where(d => d.ho_ten.Contains(searchTerm));
            }

            var result = directors.OrderBy(d => d.ho_ten).ToList();
            ViewBag.SearchTerm = searchTerm;
            return View(result);
        }

        // GET: Admin/DirectorManagement/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/DirectorManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Dao_Dien director, HttpPostedFileBase imageFile, string imageUrl)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Handle image upload or URL
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(imageFile.FileName);
                        var uploadDir = Server.MapPath("~/Content/images/directors");

                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }

                        var path = Path.Combine(uploadDir, fileName);
                        imageFile.SaveAs(path);
                        director.hinh_anh = "~/Content/images/directors/" + fileName;
                    }
                    else if (!string.IsNullOrEmpty(imageUrl))
                    {
                        director.hinh_anh = imageUrl;
                    }

                    db.Dao_Diens.InsertOnSubmit(director);
                    db.SubmitChanges();

                    TempData["SuccessMessage"] = "Thêm đạo diễn thành công!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return View(director);
        }

        // GET: Admin/DirectorManagement/Edit/5
        public ActionResult Edit(int id)
        {
            var director = db.Dao_Diens.FirstOrDefault(d => d.daodien_id == id);
            if (director == null)
            {
                return HttpNotFound();
            }
            return View(director);
        }

        // POST: Admin/DirectorManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Dao_Dien director, HttpPostedFileBase imageFile, string imageUrl)
        {
            try
            {
                var existingDirector = db.Dao_Diens.FirstOrDefault(d => d.daodien_id == id);
                if (existingDirector == null)
                {
                    return HttpNotFound();
                }

                existingDirector.ho_ten = director.ho_ten;
                existingDirector.ngay_sinh = director.ngay_sinh;
                existingDirector.quoc_tich = director.quoc_tich;
                existingDirector.tieu_su = director.tieu_su;

                // Handle image upload or URL
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(imageFile.FileName);
                    var uploadDir = Server.MapPath("~/Content/images/directors");

                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    var path = Path.Combine(uploadDir, fileName);
                    imageFile.SaveAs(path);
                    existingDirector.hinh_anh = "~/Content/images/directors/" + fileName;
                }
                else if (!string.IsNullOrEmpty(imageUrl))
                {
                    existingDirector.hinh_anh = imageUrl;
                }

                db.SubmitChanges();

                TempData["SuccessMessage"] = "Cập nhật đạo diễn thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return View(director);
        }

        // POST: Admin/DirectorManagement/Delete/5
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                var director = db.Dao_Diens.FirstOrDefault(d => d.daodien_id == id);
                if (director == null)
                {
                    return Json(new { success = false, message = "Đạo diễn không tồn tại." });
                }

                // Check if director has movies
                if (director.Phims.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa đạo diễn đã có phim." });
                }

                db.Dao_Diens.DeleteOnSubmit(director);
                db.SubmitChanges();

                return Json(new { success = true, message = "Xóa đạo diễn thành công!" });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = "Có lõi xảy ra: " + ex.Message });
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
