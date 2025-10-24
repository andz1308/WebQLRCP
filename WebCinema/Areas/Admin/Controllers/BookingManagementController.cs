using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Admin")]
    public class BookingManagementController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/BookingManagement
        public ActionResult Index(string searchDate, string status)
        {
            var bookings = db.Dat_Ves.AsQueryable();

            // Filter by date
            if (!string.IsNullOrEmpty(searchDate) && DateTime.TryParse(searchDate, out DateTime date))
            {
                bookings = bookings.Where(b => b.ngay_tao.HasValue && b.ngay_tao.Value.Date == date.Date);
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                bookings = bookings.Where(b => b.trang_thai_Dat_Ve == status);
            }

            var result = bookings
                .OrderByDescending(b => b.ngay_tao)
                .ToList();

            ViewBag.SearchDate = searchDate;
            ViewBag.Status = status;

            return View(result);
        }

        // GET: Admin/BookingManagement/Details/5
        public ActionResult Details(int id)
        {
            var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == id);
            if (booking == null)
            {
                return HttpNotFound();
            }

            ViewBag.Tickets = booking.Ves.ToList();
            ViewBag.Foods = booking.DonHang_DoAns.ToList();

            return View(booking);
        }

        // POST: Admin/BookingManagement/UpdateStatus
        [HttpPost]
        public ActionResult UpdateStatus(int id, string status)
        {
            try
            {
                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tồn tại." });
                }

                booking.trang_thai_Dat_Ve = status;
                db.SubmitChanges();

                // Send email notification
                // TODO: Implement email service

                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST: Admin/BookingManagement/Cancel
        [HttpPost]
        public ActionResult Cancel(int id)
        {
            try
            {
                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tòn tại." });
                }

                booking.trang_thai_Dat_Ve = "Đã hủy";
                
                // Update ticket status
                foreach (var ticket in booking.Ves)
                {
                    ticket.trang_thai_ve = "Đã hủy";
                }

                db.SubmitChanges();

                return Json(new { success = true, message = "Hủy thành công!" });
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
