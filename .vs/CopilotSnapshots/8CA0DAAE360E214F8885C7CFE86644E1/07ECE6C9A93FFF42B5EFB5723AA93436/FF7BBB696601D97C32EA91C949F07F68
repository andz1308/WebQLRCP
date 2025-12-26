using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Admin,Staff")]
    public class TicketManagementController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/TicketManagement
        public ActionResult Index(int? customerId, int? movieId, int? cinemaId, DateTime? date, string ticketStatus, string bookingPaymentStatus, DateTime? fromDate, DateTime? toDate, string searchText, int? page)
        {
            var showtimes = db.Suat_Chieus.AsQueryable();

            // Filter by movie
            if (movieId.HasValue)
                showtimes = showtimes.Where(s => s.phim_id == movieId.Value);

            // Filter by cinema (through room)
            if (cinemaId.HasValue)
                showtimes = showtimes.Where(s => s.Phong_Chieu.rap_id == cinemaId.Value);

            // Filter by specific date
            if (date.HasValue)
                showtimes = showtimes.Where(s => s.ngay_chieu == date.Value);

            // Filter by date range
            if (fromDate.HasValue)
                showtimes = showtimes.Where(s => s.ngay_chieu >= fromDate.Value);

            if (toDate.HasValue)
                showtimes = showtimes.Where(s => s.ngay_chieu <= toDate.Value);

            // Filter by ticket status (if needed)
            if (!string.IsNullOrEmpty(ticketStatus))
            {
                showtimes = showtimes.Where(s => s.Ves.Any(v => v.trang_thai_ve == ticketStatus));
            }

            // Filter by booking payment status (if needed)
            if (!string.IsNullOrEmpty(bookingPaymentStatus))
            {
                showtimes = showtimes.Where(s => s.Ves.Any(v =>
                    v.Dat_Ve != null && v.Dat_Ve.trang_thai_Dat_Ve == bookingPaymentStatus));
            }

            // Search by customer name or movie name
            if (!string.IsNullOrEmpty(searchText))
            {
                searchText = searchText.ToLower();
                showtimes = showtimes.Where(s =>
                    s.Phim.ten_phim.ToLower().Contains(searchText) ||
                    s.Ves.Any(v => v.Dat_Ve != null && v.Dat_Ve.Khach_Hang.ho_ten.ToLower().Contains(searchText))
                );
            }

            var allShowtimes = showtimes
                .OrderByDescending(s => s.ngay_chieu)
                .ThenBy(s => s.Ca_Chieu.gio_bat_dau)
                .ToList();

            // **Update expired tickets automatically**
            var now = DateTime.Now;
            foreach (var showtime in allShowtimes)
            {
                foreach (var ticket in showtime.Ves)
                {
                    // If showtime date has passed and ticket is unused => expired
                    if (showtime.ngay_chieu < now.Date && ticket.trang_thai_ve == "Chưa sử dụng")
                    {
                        ticket.trang_thai_ve = "Đã hết hạn";
                    }
                }
            }
            db.SubmitChanges();

            // ✅ PHÂN TRANG - 10 items per page
            int pageSize = 10;
            int pageNumber = page ?? 1;
            int totalShowtimes = allShowtimes.Count;
            int totalPages = (int)Math.Ceiling(totalShowtimes / (double)pageSize);

            var pagedShowtimes = allShowtimes
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Customers = new SelectList(db.Khach_Hangs, "khach_hang_id", "ho_ten", customerId);
            ViewBag.Movies = new SelectList(db.Phims, "phim_id", "ten_phim", movieId);
            ViewBag.Cinemas = new SelectList(db.Raps, "rap_id", "ten_rap", cinemaId);

            // **TICKET STATUS (Trạng thái Vé)**
            ViewBag.TicketStatuses = new SelectList(new[]
            {
                new { value = "Chưa sử dụng", text = "Chưa sử dụng" },
                new { value = "Đã sử dụng", text = "Đã sử dụng" },
                new { value = "Đã hết hạn", text = "Đã hết hạn" },
                new { value = "Đã Hủy", text = "Đã Hủy" }
            }, "value", "text", ticketStatus);

            // **BOOKING PAYMENT STATUS (Trạng thái Thanh toán Đặt Vé)**
            ViewBag.BookingPaymentStatuses = new SelectList(new[]
            {
                new { value = "Chưa thanh toán", text = "Chưa thanh toán" },
                new { value = "Đã Thanh toán", text = "Đã Thanh toán" },
                new { value = "Đã Hủy", text = "Đã Hủy" }
            }, "value", "text", bookingPaymentStatus);

            // ✅ Pass filter values and pagination info
            ViewBag.CustomerId = customerId;
            ViewBag.MovieId = movieId;
            ViewBag.CinemaId = cinemaId;
            ViewBag.Date = date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.TicketStatus = ticketStatus;
            ViewBag.BookingPaymentStatus = bookingPaymentStatus;
            ViewBag.FromDate = fromDate.HasValue ? fromDate.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.ToDate = toDate.HasValue ? toDate.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.SearchText = searchText;
            
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalShowtimes = totalShowtimes;

            return View(pagedShowtimes);
        }

        // GET: Admin/TicketManagement/Details/5
        public ActionResult Details(int id)
        {
            var showtime = db.Suat_Chieus.FirstOrDefault(s => s.suat_chieu_id == id);
            if (showtime == null)
                return HttpNotFound();

            // Get all tickets for this showtime
            var tickets = showtime.Ves.ToList();

            // Calculate statistics
            var totalTickets = tickets.Count;
            var availableTickets = tickets.Count(v => v.Dat_Ve_id == null && v.trang_thai_ve == "Chưa sử dụng");
            var bookedTickets = tickets.Count(v =>
                v.Dat_Ve_id != null &&
                v.Dat_Ve != null &&
                v.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán" &&
                v.trang_thai_ve != "Đã Hủy"
            );
            var usedTickets = tickets.Count(v => v.trang_thai_ve == "Đã sử dụng");
            var pendingTickets = tickets.Count(v =>
                v.Dat_Ve_id != null &&
                v.Dat_Ve != null &&
                v.Dat_Ve.trang_thai_Dat_Ve == "Chưa thanh toán"
            );

            // Set ViewBag
            ViewBag.Showtime = showtime;
            ViewBag.Tickets = tickets;
            ViewBag.TotalTickets = totalTickets;
            ViewBag.AvailableTickets = availableTickets;
            ViewBag.BookedTickets = bookedTickets;
            ViewBag.UsedTickets = usedTickets;
            ViewBag.PendingTickets = pendingTickets;

            return View(showtime);
        }

        // POST: Admin/TicketManagement/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string newStatus)
        {
            try
            {
                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == id);
                if (booking == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

                // **Validate payment status (Trạng thái Thanh toán)**
                var validStatuses = new[] { "Đã Thanh toán", "Chưa thanh toán", "Đã Hủy" };
                if (!validStatuses.Contains(newStatus))
                    return Json(new { success = false, message = "Trạng thái không hợp lệ" });

                booking.trang_thai_Dat_Ve = newStatus;
                db.SubmitChanges();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/TicketManagement/Delete
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == id);
                if (booking == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

                // Delete related data
                var tickets = db.Ves.Where(v => v.Dat_Ve_id == id).ToList();
                foreach (var ticket in tickets)
                {
                    ticket.Dat_Ve_id = null;
                    ticket.trang_thai_ve = "Chưa sử dụng";
                    // ❌ KHÔNG SET NULL - tránh unique constraint violation
                    // ticket.ma_qr_code = null;
                }

                var foodOrders = db.DonHang_DoAns.Where(d => d.Dat_Ve_id == id).ToList();
                db.DonHang_DoAns.DeleteAllOnSubmit(foodOrders);

                db.Dat_Ves.DeleteOnSubmit(booking);
                db.SubmitChanges();

                return Json(new { success = true, message = "Xóa đơn hàng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/TicketManagement/DeleteAllTickets
        [HttpPost]
        public ActionResult DeleteAllTickets(int id)
        {
            try
            {
                var showtime = db.Suat_Chieus.FirstOrDefault(s => s.suat_chieu_id == id);
                if (showtime == null)
                    return Json(new { success = false, message = "Không tìm thấy suất chiếu" });

                // Get all tickets for this showtime
                var tickets = showtime.Ves.ToList();

                // Check if any tickets have been used
                var usedTickets = tickets.Where(v => v.trang_thai_ve == "Đã sử dụng").ToList();
                if (usedTickets.Any())
                    return Json(new { success = false, message = "Không thể xóa suất chiếu có vé đã sử dụng!" });

                // Delete all tickets
                foreach (var ticket in tickets)
                {
                    // Delete related food orders
                    var foodOrders = db.DonHang_DoAns.Where(d => d.Dat_Ve_id == ticket.Dat_Ve_id).ToList();
                    db.DonHang_DoAns.DeleteAllOnSubmit(foodOrders);

                    // Delete the ticket
                    db.Ves.DeleteOnSubmit(ticket);
                }

                // Delete all related bookings
                var bookings = db.Dat_Ves.Where(b => b.Ves.Any(v => v.suat_chieu_id == id)).ToList();
                db.Dat_Ves.DeleteAllOnSubmit(bookings);

                // Delete the showtime
                db.Suat_Chieus.DeleteOnSubmit(showtime);
                db.SubmitChanges();

                return Json(new { success = true, message = "Xóa suất chiếu và tất cả vé thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // GET: Admin/TicketManagement/Report
        public ActionResult Report(DateTime? fromDate, DateTime? toDate)
        {
            var query = db.Dat_Ves.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(b => b.ngay_tao >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(b => b.ngay_tao <= toDate.Value.AddDays(1));

            var bookings = query.ToList();

            ViewBag.TotalBookings = bookings.Count;
            ViewBag.TotalRevenue = bookings.Sum(b => (decimal?)b.tong_tien) ?? 0;
            ViewBag.PaidBookings = bookings.Count(b => b.trang_thai_Dat_Ve == "Đã Thanh toán");
            ViewBag.UnpaidBookings = bookings.Count(b => b.trang_thai_Dat_Ve == "Chưa thanh toán");
            ViewBag.CancelledBookings = bookings.Count(b => b.trang_thai_Dat_Ve == "Đã Hủy");

            ViewBag.FromDate = fromDate.HasValue ? fromDate.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.ToDate = toDate.HasValue ? toDate.Value.ToString("yyyy-MM-dd") : null;

            return View(bookings);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}