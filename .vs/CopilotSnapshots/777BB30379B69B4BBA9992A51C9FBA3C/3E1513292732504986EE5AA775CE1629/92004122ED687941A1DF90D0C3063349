using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Admin")]
    public class ReviewManagementController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/ReviewManagement
        public ActionResult Index(int? movieId, string status)
        {
            var reviews = db.Danh_Gias.AsQueryable();

            // Filter by movie
            if (movieId.HasValue)
            {
                reviews = reviews.Where(r => r.phim_id == movieId.Value);
            }

            var result = reviews
                .OrderByDescending(r => r.ngay_Danh_Gia)
                .ToList();

            ViewBag.Movies = new SelectList(db.Phims, "phim_id", "ten_phim");
            ViewBag.SelectedMovie = movieId;

            return View(result);
        }

        // POST: Admin/ReviewManagement/Delete/5
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                var review = db.Danh_Gias.FirstOrDefault(r => r.Danh_Gia_id == id);
                if (review == null)
                {
                    return Json(new { success = false, message = "Đánh giá không tồn tại." });
                }

                db.Danh_Gias.DeleteOnSubmit(review);
                db.SubmitChanges();

                return Json(new { success = true, message = "Xóa Đánh giá thành công!" });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
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
