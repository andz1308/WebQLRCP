using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebCinema.Services;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Controllers
{
    public class HomeController : Controller
    {
        private MovieService movieService = new MovieService();
        private CSDLDataContext db = new CSDLDataContext();

        public ActionResult Index(int? cinemaId, int? genreId, string language, DateTime? date, int? page)//Trang chủ
        {
            // Determine date filter: if provided use it, otherwise default to today
            DateTime today = DateTime.Today;
            DateTime filterDate = date.HasValue ? date.Value.Date : today;

            // ✅ THÊM: Lấy Top 3 Phim Hot (Doanh thu + Vé bán cao nhất trong tháng HIỆN TẠI)
            // ✅ QUAN TRỌNG: Tính theo NGÀY CHIẾU (sc.ngay_chieu), KHÔNG phải ngày đặt vé
            var currentMonth = DateTime.Now.Month;  // Tháng hiện tại (12)
            var currentYear = DateTime.Now.Year;    // Năm hiện tại (2024 hoặc 2025)
            
            // ✅ Tính khoảng thời gian của tháng hiện tại
            var startMonth = new DateTime(currentYear, currentMonth, 1);  // Ngày đầu tháng hiện tại
            var endMonth = startMonth.AddMonths(1);  // Ngày đầu tháng tiếp theo

            // ✅ DEBUG: Log để kiểm tra
            System.Diagnostics.Debug.WriteLine($"🔍 Hot Movies Query - Current Month: {currentMonth}/{currentYear}");
            System.Diagnostics.Debug.WriteLine($"🔍 Date Range: {startMonth:yyyy-MM-dd} to {endMonth:yyyy-MM-dd}");
            System.Diagnostics.Debug.WriteLine($"⚠️ FILTERING BY SHOWTIME DATE (sc.ngay_chieu), NOT booking date!");

            // ✅ Query để lấy phim hot TRONG THÁNG HIỆN TẠI
            // ✅ SỬA: Lọc theo NGÀY CHIẾU (sc.ngay_chieu) thay vì ngày đặt vé (dv.ngay_tao)
            var hotMovies = (from v in db.Ves
                             join dv in db.Dat_Ves on v.Dat_Ve_id equals dv.Dat_Ve_id
                             join sc in db.Suat_Chieus on v.suat_chieu_id equals sc.suat_chieu_id
                             join p in db.Phims on sc.phim_id equals p.phim_id
                             where dv.trang_thai_Dat_Ve == "Đã Thanh toán"
                                   && sc.ngay_chieu >= startMonth          // ✅ Lọc theo NGÀY CHIẾU từ đầu tháng
                                   && sc.ngay_chieu < endMonth             // ✅ Lọc theo NGÀY CHIẾU đến cuối tháng
                             group new { v } by new
                             {
                                 p.phim_id,
                                 p.ten_phim,
                                 p.hinh_anh,
                                 p.thoi_luong
                             } into g
                             select new
                             {
                                 PhimId = g.Key.phim_id,
                                 TenPhim = g.Key.ten_phim,
                                 HinhAnh = g.Key.hinh_anh,
                                 ThoiLuong = g.Key.thoi_luong,
                                 SoVeBan = g.Count(),
                                 // ✅ DOANH THU TRONG THÁNG: Tính theo ngày chiếu trong tháng hiện tại
                                 DoanhThuThangNay = g.Sum(x => x.v.gia_ve)
                             })
                             .OrderByDescending(x => x.DoanhThuThangNay)  // ✅ Sắp xếp theo doanh thu
                             .ThenByDescending(x => x.SoVeBan)             // ✅ Nếu bằng nhau thì xét số vé
                             .Take(3)
                             .ToList();

            // ✅ DEBUG: Log kết quả
            System.Diagnostics.Debug.WriteLine($"🎬 Found {hotMovies.Count} hot movies with showtimes in month {currentMonth}/{currentYear}");
            foreach (var hm in hotMovies)
            {
                System.Diagnostics.Debug.WriteLine($"   - {hm.TenPhim}: {hm.DoanhThuThangNay:N0} VND ({hm.SoVeBan} vé)");
            }

            // Tạo ViewModel cho Hot Movies
            var hotMoviesViewModel = hotMovies.Select(hm => new MovieViewModel
            {
                Movie = db.Phims.FirstOrDefault(p => p.phim_id == hm.PhimId),
                Genres = movieService.GetMovieGenres(hm.PhimId),
                AverageRating = movieService.GetAverageRating(hm.PhimId),
                RatingCount = movieService.GetRatingCount(hm.PhimId),
                ImagePath = ResolveImagePath(hm.HinhAnh),
                // Thêm thông tin hot movie
                SoVeBan = hm.SoVeBan,
                TongDoanhThu = (decimal)hm.DoanhThuThangNay  // ✅ Doanh thu theo ngày chiếu tháng hiện tại
            }).ToList();

            ViewBag.HotMovies = hotMoviesViewModel;
            ViewBag.HotMoviesMonth = currentMonth;  // ✅ Truyền tháng hiện tại ra View
            ViewBag.HotMoviesYear = currentYear;    // ✅ Truyền năm hiện tại ra View

            // Build showtime query depending on whether a specific date was requested
            var showtimeQuery = db.Suat_Chieus.AsQueryable();

            if (date.HasValue)
            {
                showtimeQuery = showtimeQuery.Where(sc => sc.ngay_chieu == filterDate);
            }
            else
            {
                // default: future showtimes starting from today
                showtimeQuery = showtimeQuery.Where(sc => sc.ngay_chieu >= today);
            }

            // Apply cinema filter to showtime query if present
            if (cinemaId.HasValue && cinemaId.Value > 0)
            {
                var roomIds = db.Phong_Chieus
                    .Where(pc => pc.rap_id == cinemaId.Value)
                    .Select(pc => pc.phong_chieu_id)
                    .ToList();

                showtimeQuery = showtimeQuery.Where(sc => roomIds.Contains(sc.phong_chieu_id));
            }

            // Apply language filter to showtime query if present
            if (!string.IsNullOrEmpty(language))
            {
                showtimeQuery = showtimeQuery.Where(sc => sc.ngon_ngu == language);
            }

            // Now get movie IDs that have matching showtimes
            var movieIdsWithShowtimes = showtimeQuery
                .Select(sc => sc.phim_id)
                .Distinct()
                .ToList();

            // If genre filter is present, intersect with genre list
            List<Phim> movies;
            if (genreId.HasValue && genreId.Value > 0)
            {
                var movieIdsWithGenre = db.Phim_LoaiPhims
                    .Where(plp => plp.loaiphim_id == genreId.Value)
                    .Select(plp => plp.phim_id)
                    .ToList();

                movies = db.Phims
                    .Where(p => movieIdsWithShowtimes.Contains(p.phim_id) && movieIdsWithGenre.Contains(p.phim_id))
                    .ToList();
            }
            else
            {
                movies = db.Phims
                    .Where(p => movieIdsWithShowtimes.Contains(p.phim_id))
                    .ToList();
            }

            // Tạo danh sách ViewModel để truyền thêm thông tin
            var movieViewModels = movies.Select(m => new MovieViewModel
            {
                Movie = m,
                Genres = movieService.GetMovieGenres(m.phim_id),
                AverageRating = movieService.GetAverageRating(m.phim_id),
                RatingCount = movieService.GetRatingCount(m.phim_id),
                ImagePath = ResolveImagePath(m.hinh_anh)
            })
            .ToList();

            // ✅ PHÂN TRANG - 10 phim mỗi trang
            int pageSize = 10;
            int pageNumber = page ?? 1;
            int totalMovies = movieViewModels.Count;
            int totalPages = (int)Math.Ceiling(totalMovies / (double)pageSize);

            var pagedMovies = movieViewModels
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Truyền thông tin phân trang
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalMovies = totalMovies;

            // ✅ CHỈ LẤY CÁC PROPERTY CẦN THIẾT (tránh circular reference)
            var cinemas = db.Raps
                .OrderBy(r => r.ten_rap)
                .Select(r => new
                {
                    rap_id = r.rap_id,
                    ten_rap = r.ten_rap,
                    dia_chi = r.dia_chi,
                    mo_ta = r.mo_ta,
                    email = r.email
                })
                .ToList();
            ViewBag.Cinemas = cinemas;

            // ✅ CHỈ LẤY CÁC PROPERTY CẦN THIẾT
            string[] excludedRatings1 = { "P", "K", "T13", "T16", "T18", "C", "18+", "16+", "13+" };

            // 2. Query DB và lọc bỏ các mã trên
            var genres = db.Loai_Phims
                .Where(g => !excludedRatings1.Contains(g.ten_loai)) // 👈 Thêm dòng này: Chỉ lấy cái nào KHÔNG nằm trong danh sách loại trừ
                .OrderBy(g => g.ten_loai)
                .Select(g => new
                {
                    loaiphim_id = g.loaiphim_id,
                    ten_loai = g.ten_loai
                })
                .ToList();

            ViewBag.Genres = genres;

            // ✅ Load danh sách ngôn ngữ từ suất chiếu matching our showtimeQuery (respect selected date)
            var languages = showtimeQuery
                .Where(sc => sc.ngon_ngu != null && sc.ngon_ngu != "")
                .Select(sc => sc.ngon_ngu)
                .Distinct()
                .OrderBy(l => l)
                .ToList();

            ViewBag.Languages = languages;

            // Lưu giá trị filter hiện tại
            ViewBag.SelectedCinemaId = cinemaId;
            ViewBag.SelectedGenreId = genreId;
            ViewBag.SelectedLanguage = language;
            ViewBag.SelectedDate = date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null;

            return View(pagedMovies);
        }

        // ✅ Hiển thị phim đang chiếu theo rạp
        public ActionResult CinemaMovies(int? cinemaId)
        {
            if (!cinemaId.HasValue)
            {
                return RedirectToAction("Index");
            }

            try
            {
                var cinema = db.Raps.FirstOrDefault(r => r.rap_id == cinemaId.Value);
                if (cinema == null)
                {
                    return HttpNotFound();
                }

                // Lấy tất cả phòng chiếu của rạp này
                var roomIds = db.Phong_Chieus
                    .Where(pc => pc.rap_id == cinemaId.Value)
                    .Select(pc => pc.phong_chieu_id)
                    .ToList();

                // Lấy các phim có suất chiếu trong các phòng của rạp này (từ hôm nay trở đi)
                var today = DateTime.Today;
                var movieIdsWithShowtimes = db.Suat_Chieus
                    .Where(sc => roomIds.Contains(sc.phong_chieu_id) && sc.ngay_chieu >= today)
                    .Select(sc => sc.phim_id)
                    .Distinct()
                    .ToList();

                // Lấy thông tin chi tiết các phim
                var movies = db.Phims
                    .Where(p => movieIdsWithShowtimes.Contains(p.phim_id))
                    .OrderBy(p => p.ten_phim)
                    .ToList();

                // Tạo ViewModel
                var movieViewModels = movies.Select(m => new MovieViewModel
                {
                    Movie = m,
                    Genres = movieService.GetMovieGenres(m.phim_id),
                    AverageRating = movieService.GetAverageRating(m.phim_id),
                    RatingCount = movieService.GetRatingCount(m.phim_id),
                    ImagePath = ResolveImagePath(m.hinh_anh)
                }).ToList();

                ViewBag.CinemaName = cinema.ten_rap;
                ViewBag.CinemaId = cinemaId.Value;

                return View(movieViewModels);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách phim.";
                return RedirectToAction("Index");
            }
        }

        public ActionResult Detail(int? id)//Trang chi tiết phim
        {
            // Nếu không có id thì chuyển hướng về trang chính (tránh lỗi khi truy cập /Home/Detail trực tiếp)
            if (!id.HasValue)
            {
                return RedirectToAction("Index");
            }

            var vm = movieService.GetMovieDetailViewModel(id.Value);
            if (vm == null) return HttpNotFound();

            // Resolve image path for detail view as well
            vm.ImagePath = ResolveImagePath(vm.ImagePath);

            return View(vm);
        }

        [HttpGet]
        public JsonResult Search(string q)
        {
            try
            {
                var term = (q ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(term))
                {
                    return Json(new object[0], JsonRequestBehavior.AllowGet);
                }

                // Only movies that have future or today showtimes
                var nowShowing = movieService.GetNowShowingMovies();
                var results = nowShowing
                    .Where(m => m.ten_phim != null && m.ten_phim.IndexOf(term, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    .Where(m => {
                        var sts = movieService.GetMovieShowtimes(m.phim_id) ?? new List<Suat_Chieu>();
                        return sts.Any(sc => sc.ngay_chieu >= DateTime.Today);
                    })
                    .Select(m => new {
                        id = m.phim_id,
                        name = m.ten_phim,
                        image = ResolveImagePath(m.hinh_anh)
                    })
                    .Take(10)
                    .ToList();

                return Json(results, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Never throw to client; return empty
                return Json(new object[0], JsonRequestBehavior.AllowGet);
            }
        }

        private string ResolveImagePath(string rawPath)
        {
            if (string.IsNullOrEmpty(rawPath)) return null;

            // If already an absolute URL
            if (rawPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) || rawPath.StartsWith("//"))
            {
                return rawPath;
            }

            // If virtual path like ~/...
            if (rawPath.StartsWith("~"))
            {
                return Url.Content(rawPath);
            }

            // Else assume stored filename in Content/images/movies/
            return Url.Content("~/Content/images/movies/" + rawPath);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
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