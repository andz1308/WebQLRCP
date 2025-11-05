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
        public ActionResult Index(int? customerId, int? movieId, string ticketStatus, string bookingPaymentStatus, DateTime? fromDate, DateTime? toDate, string searchText, int? pageNumber)
        {
            var bookings = db.Dat_Ves.AsQueryable();

            // Search by customer name, movie name, or booking ID
            if (!string.IsNullOrEmpty(searchText))
            {
                searchText = searchText.ToLower();
                bookings = bookings.Where(b => 
                    (b.Khach_Hang.ho_ten.ToLower().Contains(searchText)) ||
                    (b.Ves.Any(v => v.Suat_Chieu.Phim.ten_phim.ToLower().Contains(searchText))) ||
                    (b.Dat_Ve_id.ToString().Contains(searchText))
                );
            }

            // Filter by customer
            if (customerId.HasValue)
                bookings = bookings.Where(b => b.khach_hang_id == customerId.Value);

            // **FILTER BY TICKET STATUS (Tr?ng thái Vé) - not booking status**
            if (!string.IsNullOrEmpty(ticketStatus))
            {
                bookings = bookings.Where(b => b.Ves.Any(v => v.trang_thai_ve == ticketStatus));
            }

            // **FILTER BY BOOKING PAYMENT STATUS (Tr?ng thái Thanh toán ??t Vé)**
            if (!string.IsNullOrEmpty(bookingPaymentStatus))
            {
                bookings = bookings.Where(b => b.trang_thai_Dat_Ve == bookingPaymentStatus);
            }

            // Filter by date range
            if (fromDate.HasValue)
                bookings = bookings.Where(b => b.ngay_tao >= fromDate.Value);

            if (toDate.HasValue)
                bookings = bookings.Where(b => b.ngay_tao <= toDate.Value.AddDays(1));

            // Filter by movie (through tickets)
            if (movieId.HasValue)
                bookings = bookings.Where(b => b.Ves.Any(v => v.Suat_Chieu.phim_id == movieId.Value));

            var result = bookings
                .OrderByDescending(b => b.ngay_tao)
                .ToList();

            // **Update expired tickets automatically**
            var now = DateTime.Now;
            foreach (var booking in result)
            {
                foreach (var ticket in booking.Ves)
                {
                    // N?u su?t chi?u ?ã qua ngày hôm nay và vé ch?a s? d?ng => ?ã h?t h?n
                    if (ticket.Suat_Chieu != null && 
                        ticket.Suat_Chieu.ngay_chieu < now.Date && 
                        ticket.trang_thai_ve == "Ch?a s? d?ng")
                    {
                        ticket.trang_thai_ve = "?ã h?t h?n";
                    }
                }
            }
            db.SubmitChanges();

            ViewBag.Customers = new SelectList(db.Khach_Hangs, "khach_hang_id", "ho_ten", customerId);
            ViewBag.Movies = new SelectList(db.Phims, "phim_id", "ten_phim", movieId);
            
            // **TICKET STATUS (Tr?ng thái Vé)**
            ViewBag.TicketStatuses = new SelectList(new[] 
            { 
                new { value = "Ch?a s? d?ng", text = "Ch?a s? d?ng" },
                new { value = "?ã s? d?ng", text = "?ã s? d?ng" },
                new { value = "?ã h?t h?n", text = "?ã h?t h?n" },
                new { value = "?ã H?y", text = "?ã H?y" }
            }, "value", "text", ticketStatus);

            // **BOOKING PAYMENT STATUS (Tr?ng thái Thanh toán ??t Vé)**
            ViewBag.BookingPaymentStatuses = new SelectList(new[]
            {
                new { value = "Ch?a thanh toán", text = "Ch?a thanh toán" },
                new { value = "?ã Thanh toán", text = "?ã Thanh toán" },
                new { value = "?ã H?y", text = "?ã H?y" }
            }, "value", "text", bookingPaymentStatus);

            ViewBag.CustomerId = customerId;
            ViewBag.MovieId = movieId;
            ViewBag.TicketStatus = ticketStatus;
            ViewBag.BookingPaymentStatus = bookingPaymentStatus;
            ViewBag.FromDate = fromDate.HasValue ? fromDate.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.ToDate = toDate.HasValue ? toDate.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.SearchText = searchText;
            ViewBag.PageNumber = pageNumber ?? 1;

            return View(result);
        }

        // GET: Admin/TicketManagement/Details/5
        public ActionResult Details(int id)
        {
            var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == id);
            if (booking == null)
                return HttpNotFound();

            return View(booking);
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
                    return Json(new { success = false, message = "Không tìm th?y ??n hàng" });

                // **Validate payment status (Tr?ng thái Thanh toán)**
                var validStatuses = new[] { "?ã Thanh toán", "Ch?a thanh toán", "?ã H?y" };
                if (!validStatuses.Contains(newStatus))
                    return Json(new { success = false, message = "Tr?ng thái không h?p l?" });

                booking.trang_thai_Dat_Ve = newStatus;
                db.SubmitChanges();

                return Json(new { success = true, message = "C?p nh?t tr?ng thái thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "L?i: " + ex.Message });
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
                    return Json(new { success = false, message = "Không tìm th?y ??n hàng" });

                // Delete related data
                var tickets = db.Ves.Where(v => v.Dat_Ve_id == id).ToList();
                foreach (var ticket in tickets)
                {
                    ticket.Dat_Ve_id = null;
                    ticket.trang_thai_ve = "Ch?a s? d?ng";
                    ticket.ma_qr_code = null;
                }

                var foodOrders = db.DonHang_DoAns.Where(d => d.Dat_Ve_id == id).ToList();
                db.DonHang_DoAns.DeleteAllOnSubmit(foodOrders);

                db.Dat_Ves.DeleteOnSubmit(booking);
                db.SubmitChanges();

                return Json(new { success = true, message = "Xóa ??n hàng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "L?i: " + ex.Message });
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
            ViewBag.PaidBookings = bookings.Count(b => b.trang_thai_Dat_Ve == "?ã Thanh toán");
            ViewBag.UnpaidBookings = bookings.Count(b => b.trang_thai_Dat_Ve == "Ch?a thanh toán");
            ViewBag.CancelledBookings = bookings.Count(b => b.trang_thai_Dat_Ve == "?ã H?y");

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
