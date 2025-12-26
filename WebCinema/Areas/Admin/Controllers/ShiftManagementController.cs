using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Admin,Staff")]
    public class ShiftManagementController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/ShiftManagement
        public ActionResult Index()
        {
            var shifts = db.Ca_Chieus.OrderBy(c => c.gio_bat_dau).ToList();
            return View(shifts);
        }

        // GET: Admin/ShiftManagement/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/ShiftManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Ca_Chieu shift)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Validate time range
                    if (shift.gio_bat_dau >= shift.gio_ket_thuc)
                    {
                        TempData["ErrorMessage"] = "Giờ kết thúc phải sau giờ bắt đầu.";
                        return View(shift);
                    }

                    // Check for overlapping shifts
                    var overlapping = db.Ca_Chieus.Any(c =>
                        (shift.gio_bat_dau >= c.gio_bat_dau && shift.gio_bat_dau < c.gio_ket_thuc) ||
                        (shift.gio_ket_thuc > c.gio_bat_dau && shift.gio_ket_thuc <= c.gio_ket_thuc) ||
                        (shift.gio_bat_dau <= c.gio_bat_dau && shift.gio_ket_thuc >= c.gio_ket_thuc));

                    if (overlapping)
                    {
                        TempData["ErrorMessage"] = "Ca chiếu bị trùng với ca chiếu khác.";
                        return View(shift);
                    }

                    db.Ca_Chieus.InsertOnSubmit(shift);
                    db.SubmitChanges();

                    TempData["SuccessMessage"] = "Thêm ca chiếu thành công!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return View(shift);
        }

        // GET: Admin/ShiftManagement/Edit/5
        public ActionResult Edit(int id)
        {
            var shift = db.Ca_Chieus.FirstOrDefault(c => c.ca_chieu_id == id);
            if (shift == null)
            {
                return HttpNotFound();
            }
            return View(shift);
        }

        // POST: Admin/ShiftManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Ca_Chieu shift)
        {
            try
            {
                var existingShift = db.Ca_Chieus.FirstOrDefault(c => c.ca_chieu_id == id);
                if (existingShift == null)
                {
                    return HttpNotFound();
                }

                // Validate time range
                if (shift.gio_bat_dau >= shift.gio_ket_thuc)
                {
                    TempData["ErrorMessage"] = "Giờ kết thúc phải sau giờ bắt đầu.";
                    return View(shift);
                }

                // Check for overlapping shifts (excluding current shift)
                var overlapping = db.Ca_Chieus.Any(c =>
                    c.ca_chieu_id != id &&
                    ((shift.gio_bat_dau >= c.gio_bat_dau && shift.gio_bat_dau < c.gio_ket_thuc) ||
                    (shift.gio_ket_thuc > c.gio_bat_dau && shift.gio_ket_thuc <= c.gio_ket_thuc) ||
                    (shift.gio_bat_dau <= c.gio_bat_dau && shift.gio_ket_thuc >= c.gio_ket_thuc)));

                if (overlapping)
                {
                    TempData["ErrorMessage"] = "Ca chiếu bị trùng với ca chiếu khác.";
                    return View(shift);
                }

                existingShift.gio_bat_dau = shift.gio_bat_dau;
                existingShift.gio_ket_thuc = shift.gio_ket_thuc;

                db.SubmitChanges();

                TempData["SuccessMessage"] = "Cập nhật ca chiếu thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return View(shift);
        }

        // POST: Admin/ShiftManagement/Delete/5
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                var shift = db.Ca_Chieus.FirstOrDefault(c => c.ca_chieu_id == id);
                if (shift == null)
                {
                    return Json(new { success = false, message = "Ca chiếu không tồn tại." });
                }

                // Check if shift has showtimes
                if (shift.Suat_Chieus.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa ca chiếu đã có suất chiếu." });
                }

                db.Ca_Chieus.DeleteOnSubmit(shift);
                db.SubmitChanges();

                return Json(new { success = true, message = "Xóa ca chiếu thành công!" });
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