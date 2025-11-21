using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Staff")]
    public class StaffShowtimeTicketManagementController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/StaffShowtimeTicketManagement
        public ActionResult Index(int? movieId, int? cinemaId, DateTime? date, int? page)
        {
            // Lấy danh sách suất chiếu
            var showtimes = db.Suat_Chieus.AsQueryable();

            // Filter by movie
            if (movieId.HasValue)
            {
                showtimes = showtimes.Where(sc => sc.phim_id == movieId.Value);
            }

            // Filter by cinema
            if (cinemaId.HasValue)
            {
                showtimes = showtimes.Where(sc => sc.Phong_Chieu != null && sc.Phong_Chieu.rap_id == cinemaId.Value);
            }

            // Filter by date
            if (date.HasValue)
            {
                var targetDate = date.Value.Date;
                showtimes = showtimes.Where(sc => sc.ngay_chieu.Date == targetDate);
            }

            // Phân trang - 10 suất chiếu mỗi trang
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var allShowtimes = showtimes
                .OrderByDescending(sc => sc.ngay_chieu)
                .ThenBy(sc => sc.ca_chieu_id)
                .ToList();

            int totalShowtimes = allShowtimes.Count;
            int totalPages = (int)Math.Ceiling(totalShowtimes / (double)pageSize);

            var result = allShowtimes
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Truyền thông tin cho View
            ViewBag.Movies = new SelectList(db.Phims, "phim_id", "ten_phim", movieId);
            ViewBag.Cinemas = new SelectList(db.Raps, "rap_id", "ten_rap", cinemaId);
            ViewBag.MovieId = movieId;
            ViewBag.CinemaId = cinemaId;
            ViewBag.Date = date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalShowtimes = totalShowtimes;

            return View(result);
        }

        // GET: Admin/StaffShowtimeTicketManagement/Details/5
        public ActionResult Details(int id)
        {
            var showtime = db.Suat_Chieus.FirstOrDefault(sc => sc.suat_chieu_id == id);
            if (showtime == null)
            {
                return HttpNotFound();
            }

            // ✅ LẤY TẤT CẢ VÉ - KHÔNG LỌC BỎ VÉ CHƯA THANH TOÁN
            var tickets = db.Ves
                .Where(v => v.suat_chieu_id == id)
                .OrderBy(v => v.Ghe.hang)
                .ThenBy(v => v.Ghe.cot)
                .ToList();

            // Thống kê - ✅ CHỈ TÍNH VÉ ĐÃ THANH TOÁN
            ViewBag.TotalTickets = tickets.Count;
            ViewBag.AvailableTickets = tickets.Count(v => 
                v.Dat_Ve_id == null || 
                (v.Dat_Ve != null && v.Dat_Ve.trang_thai_Dat_Ve == "Chưa thanh toán"));
            
            // ✅ VÉ ĐÃ ĐẶT = ĐÃ THANH TOÁN VÀ CHƯA HỦY
            ViewBag.BookedTickets = tickets.Count(v => 
                v.Dat_Ve_id != null && 
                v.Dat_Ve != null && 
                v.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán" &&
                v.trang_thai_ve != "Đã Hủy"
            );
            
            ViewBag.UsedTickets = tickets.Count(v => v.trang_thai_ve == "Đã sử dụng");
            ViewBag.CanceledTickets = tickets.Count(v => v.trang_thai_ve == "Đã Hủy");
            ViewBag.ExpiredTickets = tickets.Count(v => v.trang_thai_ve == "Đã hết hạn");
            
            // ✅ VÉ CHỜ THANH TOÁN (để biết)
            ViewBag.PendingTickets = tickets.Count(v => 
                v.Dat_Ve_id != null && 
                v.Dat_Ve != null && 
                v.Dat_Ve.trang_thai_Dat_Ve == "Chưa thanh toán"
            );

            ViewBag.Showtime = showtime;
            ViewBag.Tickets = tickets;

            return View(showtime);
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
