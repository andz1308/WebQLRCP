using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Staff")]
    public class StaffBookingManagementController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/StaffBookingManagement/Details/5
        public ActionResult Details(int? id)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index", "StaffShowtimeTicketManagement");
            }

            try
            {
                // ? EAGER LOADING: Load t?t c? related entities
                var booking = db.Dat_Ves
                    .Where(b => b.Dat_Ve_id == id.Value)
                    .FirstOrDefault();

                if (booking == null)
                {
                    return HttpNotFound();
                }

                // ? Force load all related entities
                var khachHang = booking.Khach_Hang;
                var tickets = booking.Ves.ToList();
                var foods = booking.DonHang_DoAns.ToList();

                // ? Force load related entities for tickets
                foreach (var ticket in tickets)
                {
                    var ghe = ticket.Ghe;
                    var suatChieu = ticket.Suat_Chieu;
                    if (suatChieu != null)
                    {
                        var phim = suatChieu.Phim;
                        var phongChieu = suatChieu.Phong_Chieu;
                        var caChieu = suatChieu.Ca_Chieu;
                        if (phongChieu != null)
                        {
                            var rap = phongChieu.Rap;
                        }
                    }
                }

                // ? Force load related entities for foods
                foreach (var foodOrder in foods)
                {
                    var doAn = foodOrder.Do_An;
                }

                ViewBag.Tickets = tickets;
                ViewBag.Foods = foods;

                return View(booking);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Lỗi khi tải chi tiết đơn đặt: " + ex.Message;
                return RedirectToAction("Index", "StaffShowtimeTicketManagement");
            }
        }

        // POST: Admin/StaffBookingManagement/Cancel
        [HttpPost]
        public ActionResult Cancel(int id)
        {
            try
            {
                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tồn tại." });
                }

                booking.trang_thai_Dat_Ve = "Đã Hủy";

                // ? B??C 1: Gi?i phóng T?T C? vé c?a booking này
                var allVesInBooking = db.Ves.Where(v => v.Dat_Ve_id == id).ToList();
                foreach (var ticket in allVesInBooking)
                {
                    ticket.Dat_Ve_id = null;
                    ticket.trang_thai_ve = "Chưa sử dụng";
                    ticket.ma_qr_code = null;
                }

                // ? B??C 2: Xóa t?t c? ??n hàng ?? ?n
                var foodOrders = db.DonHang_DoAns.Where(f => f.Dat_Ve_id == id).ToList();
                foreach (var food in foodOrders)
                {
                    db.DonHang_DoAns.DeleteOnSubmit(food);
                }

                db.SubmitChanges();

                LoggingHelper.LogInfo($"? Staff Cancel: Hủy Dat_Ve ID={id}, giải phóng {allVesInBooking.Count} vé");

                return Json(new { success = true, message = "Hủy đơn đặt thành công!" });
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
