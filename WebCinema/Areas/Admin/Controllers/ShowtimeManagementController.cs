using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Admin,Staff")]
    public class ShowtimeManagementController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/ShowtimeManagement
        public ActionResult Index(int? movieId, int? cinemaId, DateTime? date)
        {
            var showtimes = db.Suat_Chieus.AsQueryable();

            // Lọc theo phim
            if (movieId.HasValue)
                showtimes = showtimes.Where(sc => sc.phim_id == movieId.Value);

            // Lọc theo rạp
            if (cinemaId.HasValue)
                showtimes = showtimes.Where(sc => sc.Phong_Chieu.rap_id == cinemaId.Value);

            // Lọc theo ngày
            if (date.HasValue)
            {
                var targetDate = date.Value.Date;
                showtimes = showtimes.Where(sc => sc.ngay_chieu.Date == targetDate);
            }

            var result = showtimes
                .OrderByDescending(sc => sc.ngay_chieu)
                .ThenBy(sc => sc.ca_chieu_id)
                .ToList();

            ViewBag.Movies = new SelectList(db.Phims, "phim_id", "ten_phim", movieId);
            ViewBag.Cinemas = new SelectList(db.Raps, "rap_id", "ten_rap", cinemaId);
            ViewBag.MovieId = movieId;
            ViewBag.CinemaId = cinemaId;
            ViewBag.Date = date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null;

            return View(result);
        }

        // GET: Admin/ShowtimeManagement/Create
        public ActionResult Create()
        {
            ViewBag.Movies = new SelectList(db.Phims, "phim_id", "ten_phim");
            ViewBag.Cinemas = new SelectList(db.Raps, "rap_id", "ten_rap");

            // Sửa lỗi: không có cột ten_ca
            ViewBag.Shifts = new SelectList(
                db.Ca_Chieus.ToList().Select(ca => new
                {
                    ca.ca_chieu_id,
                    ten_ca = $"{ca.gio_bat_dau:hh\\:mm} - {ca.gio_ket_thuc:hh\\:mm}"
                }),
                "ca_chieu_id",
                "ten_ca"
            );

            return View();
        }

        // POST: Admin/ShowtimeManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Suat_Chieu showtime, int cinemaId)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var rooms = db.Phong_Chieus.Where(pc => pc.rap_id == cinemaId).ToList();
                    if (!rooms.Any())
                    {
                        TempData["ErrorMessage"] = "Rạp này chưa có phòng chiếu nào.";
                        SetViewBags(cinemaId, showtime.phong_chieu_id);
                        return View(showtime);
                    }

                    // Kiểm tra suất chiếu trùng
                    if (db.Suat_Chieus.Any(sc => sc.phong_chieu_id == showtime.phong_chieu_id &&
                        sc.ngay_chieu == showtime.ngay_chieu &&
                        sc.ca_chieu_id == showtime.ca_chieu_id))
                    {
                        TempData["ErrorMessage"] = "Suất chiếu này đã tồn tại.";
                        SetViewBags(cinemaId, showtime.phong_chieu_id);
                        return View(showtime);
                    }

                    db.Suat_Chieus.InsertOnSubmit(showtime);
                    db.SubmitChanges();

                    TempData["SuccessMessage"] = "Thêm suất chiếu thành công!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            SetViewBags(cinemaId, showtime.phong_chieu_id);
            return View(showtime);
        }

        // GET: Admin/ShowtimeManagement/Edit/5
        public ActionResult Edit(int id)
        {
            var showtime = db.Suat_Chieus.FirstOrDefault(sc => sc.suat_chieu_id == id);
            if (showtime == null)
                return HttpNotFound();

            ViewBag.Movies = new SelectList(db.Phims, "phim_id", "ten_phim", showtime.phim_id);
            ViewBag.Cinemas = new SelectList(db.Raps, "rap_id", "ten_rap", showtime.Phong_Chieu.rap_id);
            ViewBag.Rooms = new SelectList(db.Phong_Chieus.Where(pc => pc.rap_id == showtime.Phong_Chieu.rap_id),
                "phong_chieu_id", "ten_phong", showtime.phong_chieu_id);

            ViewBag.Shifts = new SelectList(
                db.Ca_Chieus.ToList().Select(ca => new
                {
                    ca.ca_chieu_id,
                    ten_ca = $"{ca.gio_bat_dau:hh\\:mm} - {ca.gio_ket_thuc:hh\\:mm}"
                }),
                "ca_chieu_id",
                "ten_ca",
                showtime.ca_chieu_id
            );

            return View(showtime);
        }

        // POST: Admin/ShowtimeManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, FormCollection form)
        {
            try
            {
                var showtime = db.Suat_Chieus.FirstOrDefault(sc => sc.suat_chieu_id == id);
                if (showtime == null)
                    return HttpNotFound();

                showtime.phim_id = int.Parse(form["phim_id"]);
                showtime.phong_chieu_id = int.Parse(form["phong_chieu_id"]);
                showtime.ca_chieu_id = int.Parse(form["ca_chieu_id"]);

                if (!string.IsNullOrEmpty(form["ngay_chieu"]))
                    showtime.ngay_chieu = DateTime.Parse(form["ngay_chieu"]);

                db.SubmitChanges();
                TempData["SuccessMessage"] = "Cập nhật suất chiếu thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Edit", new { id });
        }

        // POST: Admin/ShowtimeManagement/Delete/5
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                var showtime = db.Suat_Chieus.FirstOrDefault(sc => sc.suat_chieu_id == id);
                if (showtime == null)
                    return Json(new { success = false, message = "Suất chiếu không tồn tại." });

                db.Suat_Chieus.DeleteOnSubmit(showtime);
                db.SubmitChanges();

                return Json(new { success = true, message = "Xóa suất chiếu thành công!" });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // AJAX: Lấy danh sách phòng theo rạp
        [HttpGet]
        public JsonResult GetRoomsByCinema(int cinemaId)
        {
            try
            {
                var rooms = db.Phong_Chieus
                    .Where(pc => pc.rap_id == cinemaId)
                    .Select(pc => new
                    {
                        value = pc.phong_chieu_id,
                        text = pc.ten_phong
                    })
                    .ToList();

                return Json(new { success = true, rooms }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private void SetViewBags(int? cinemaId, int? roomId)
        {
            ViewBag.Movies = new SelectList(db.Phims, "phim_id", "ten_phim");
            ViewBag.Cinemas = new SelectList(db.Raps, "rap_id", "ten_rap", cinemaId);
            ViewBag.Rooms = new SelectList(db.Phong_Chieus.Where(pc => pc.rap_id == cinemaId),
                "phong_chieu_id", "ten_phong", roomId);

            ViewBag.Shifts = new SelectList(
                db.Ca_Chieus.ToList().Select(ca => new
                {
                    ca.ca_chieu_id,
                    ten_ca = $"{ca.gio_bat_dau:hh\\:mm} - {ca.gio_ket_thuc:hh\\:mm}"
                }),
                "ca_chieu_id",
                "ten_ca"
            );
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
