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
        private const int PageSize = 10; // ✅ 10 phần tử mỗi trang

        // GET: Admin/BookingManagement
        public ActionResult Index(string searchDate, string ticketStatus, string bookingPaymentStatus, int? page)
        {
            var bookings = db.Dat_Ves.AsQueryable();

            // **TRẠNG THÁI VÉ HỢP LỆ (Trạng thái Bảng Ve)**
            var validTicketStatuses = new[] { "Chưa sử dụng", "Đã sử dụng", "Đã Hủy", "Đã hết hạn" };
            
            // **TRẠNG THÁI THANH TOÁN HỢP LỆ (Trạng thái Bảng Dat_Ve)**
            var validPaymentStatuses = new[] { "Chưa thanh toán", "Đã Thanh toán", "Đã Hủy" };

            // Filter by date
            if (!string.IsNullOrEmpty(searchDate) && DateTime.TryParse(searchDate, out DateTime date))
            {
                bookings = bookings.Where(b => b.ngay_tao.HasValue && b.ngay_tao.Value.Date == date.Date);
            }

            // **Filter by ticket status - VALIDATE trước**
            if (!string.IsNullOrEmpty(ticketStatus) && validTicketStatuses.Contains(ticketStatus))
            {
                bookings = bookings.Where(b => b.Ves.Any(v => v.trang_thai_ve == ticketStatus));
            }

            // **Filter by booking payment status (trạng thái thanh toán)**
            if (!string.IsNullOrEmpty(bookingPaymentStatus) && validPaymentStatuses.Contains(bookingPaymentStatus))
            {
                bookings = bookings.Where(b => b.trang_thai_Dat_Ve == bookingPaymentStatus);
            }

            // ✅ PHÂN TRANG
            int currentPage = page ?? 1;
            int totalItems = bookings.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            var result = bookings
                .OrderByDescending(b => b.ngay_tao)
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            ViewBag.SearchDate = searchDate;
            ViewBag.TicketStatus = ticketStatus;
            ViewBag.BookingPaymentStatus = bookingPaymentStatus;
            ViewBag.ValidTicketStatuses = validTicketStatuses;
            ViewBag.ValidPaymentStatuses = validPaymentStatuses;
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(result);
        }

        // GET: Admin/BookingManagement/Details/5
        public ActionResult Details(int? id)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index");
            }

            try
            {
                // ✅ EAGER LOADING: Load tất cả related entities
                var booking = db.Dat_Ves
                    .Where(b => b.Dat_Ve_id == id.Value)
                    .FirstOrDefault();

                if (booking == null)
                {
                    return HttpNotFound();
                }

                // ✅ Force load all related entities
                var khachHang = booking.Khach_Hang;
                var tickets = booking.Ves.ToList();
                var foods = booking.DonHang_DoAns.ToList();

                // ✅ Force load related entities for tickets
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

                // ✅ Force load related entities for foods
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
                return RedirectToAction("Index");
            }
        }

        // POST: Admin/BookingManagement/Delete/5
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Đơn đặt không tồn tại." });
                }

                // ✅ BƯỚC 1: Xóa tất cả vé của booking này
                var allVesInBooking = db.Ves.Where(v => v.Dat_Ve_id == id).ToList();
                foreach (var ticket in allVesInBooking)
                {
                    // ✅ Xóa đánh giá liên quan đến vé này
                    var reviews = db.Danh_Gias.Where(dg => dg.ve_id == ticket.ve_id).ToList();
                    foreach (var review in reviews)
                    {
                        db.Danh_Gias.DeleteOnSubmit(review);
                    }
                    
                    db.Ves.DeleteOnSubmit(ticket);
                }

                // ✅ BƯỚC 2: Xóa tất cả đơn hàng đồ ăn
                var foodOrders = db.DonHang_DoAns.Where(f => f.Dat_Ve_id == id).ToList();
                foreach (var food in foodOrders)
                {
                    db.DonHang_DoAns.DeleteOnSubmit(food);
                }

                // ✅ BƯỚC 3: Xóa booking
                db.Dat_Ves.DeleteOnSubmit(booking);
                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Delete: Xóa Dat_Ve ID={id}, đã xóa {allVesInBooking.Count} vé và {foodOrders.Count} đồ ăn");

                return Json(new { success = true, message = "Xóa đơn đặt thành công!" });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
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

                // Validate payment status
                var validStatuses = new[] { "Đã Thanh toán", "Chưa thanh toán", "Đã Hủy" };
                if (!validStatuses.Contains(status))
                {
                    return Json(new { success = false, message = "Trạng thái không hợp lệ" });
                }

                booking.trang_thai_Dat_Ve = status;

                // ✅ NẾU CHUYỂN SANG "ĐÃ HỦY" → QUAY LẠI GHẾ VỀ TRẠNG THÁI TRỐNG
                if (status == "Đã Hủy")
                {
                    // ✅ BƯỚC 1: Cập nhật tất cả vé của booking này
                    var allVesInBooking = db.Ves.Where(v => v.Dat_Ve_id == id).ToList();
                    foreach (var ticket in allVesInBooking)
                    {
                        ticket.Dat_Ve_id = null;
                        ticket.trang_thai_ve = "Chưa sử dụng";
                        ticket.ma_qr_code = null;
                    }

                    // ✅ BƯỚC 2: Xóa tất cả đơn hàng đồ ăn của booking này
                    var foodOrders = db.DonHang_DoAns.Where(f => f.Dat_Ve_id == id).ToList();
                    foreach (var food in foodOrders)
                    {
                        db.DonHang_DoAns.DeleteOnSubmit(food);
                    }

                    LoggingHelper.LogInfo($"✅ UpdateStatus: Hủy Dat_Ve ID={id}, giải phóng {allVesInBooking.Count} vé");
                }

                db.SubmitChanges();

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

                booking.trang_thai_Dat_Ve = "Đã Hủy";

                // ✅ BƯỚC 1: Giải phóng TẤT CẢ vé của booking này
                var allVesInBooking = db.Ves.Where(v => v.Dat_Ve_id == id).ToList();
                foreach (var ticket in allVesInBooking)
                {
                    ticket.Dat_Ve_id = null;
                    ticket.trang_thai_ve = "Chưa sử dụng";
                    ticket.ma_qr_code = null;
                }

                // ✅ BƯỚC 2: Xóa tất cả đơn hàng đồ ăn
                var foodOrders = db.DonHang_DoAns.Where(f => f.Dat_Ve_id == id).ToList();
                foreach (var food in foodOrders)
                {
                    db.DonHang_DoAns.DeleteOnSubmit(food);
                }

                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Cancel: Hủy Dat_Ve ID={id}, giải phóng {allVesInBooking.Count} vé");

                return Json(new { success = true, message = "Hủy thành công!" });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST: Admin/BookingManagement/CleanCanceledBookings
        [HttpPost]
        public ActionResult CleanCanceledBookings()
        {
            try
            {
                // Find tickets that are still linked to canceled bookings
                var stuckTickets = db.Ves
                    .Where(v => v.Dat_Ve_id != null && v.Dat_Ve.trang_thai_Dat_Ve == "Đã Hủy")
                    .ToList();

                if (!stuckTickets.Any())
                {
                    return Json(new { success = true, message = "Không có vé nào cần dọn dẹp." });
                }

                foreach (var t in stuckTickets)
                {
                    t.Dat_Ve_id = null;
                    t.trang_thai_ve = "Chưa sử dụng";
                    t.ma_qr_code = null;
                }

                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Cleaned {stuckTickets.Count} tickets linked to canceled bookings.");

                return Json(new { success = true, message = $"Đã dọn {stuckTickets.Count} vé." });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = "Có lỗi khi dọn vé: " + ex.Message });
            }
        }

        // POST: Admin/BookingManagement/ApproveBooking
        [HttpPost]
        public ActionResult ApproveBooking(int id)
        {
            try
            {
                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Đơn đặt không tồn tại." });
                }

                // Kiểm tra trạng thái
                if (booking.trang_thai_Dat_Ve != "Chờ Duyệt")
                {
                    return Json(new { success = false, message = "Chỉ có thể duyệt những đơn ở trạng thái 'Chờ Duyệt'." });
                }

                // ✅ Cập nhật trạng thái thành "Đã Thanh toán"
                booking.trang_thai_Dat_Ve = "Đã Thanh toán";

                // ✅ Cập nhật tất cả vé sang "Chưa sử dụng"
                var tickets = db.Ves.Where(v => v.Dat_Ve_id == id).ToList();
                foreach (var ticket in tickets)
                {
                    ticket.trang_thai_ve = "Chưa sử dụng";
                }

                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ ApproveBooking: Duyệt Dat_Ve ID={id}, khách hàng: {booking.Khach_Hang.ho_ten}");

                return Json(new { success = true, message = "Duyệt đơn thành công! Khách hàng sẽ được phép xem hóa đơn." });
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
