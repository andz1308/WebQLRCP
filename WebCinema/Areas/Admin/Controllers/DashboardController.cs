using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;
using System.Collections.Generic;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/Dashboard
        public ActionResult Index(DateTime? filterDate, int? filterMonth, int? filterYear)
        {
            try
            {
                // Xác định filter
                DateTime today = DateTime.Now.Date;
                int currentYear = filterYear ?? DateTime.Now.Year;
                int? currentMonth = filterMonth;

                var model = new AdminDashboardViewModel
                {
                    FilterDate = filterDate,
                    FilterMonth = filterMonth,
                    FilterYear = currentYear
                };

                // 1.1. Dashboard Tổng Hợp
                model.Summary = GetDashboardSummary(today, currentMonth, currentYear);

                // 1.2. Thống Kê Theo Rạp
                model.CinemaStatistics = GetCinemaStatistics(currentMonth, currentYear);

                // 1.3. Thống Kê Theo Phim
                model.MovieStatistics = GetMovieStatistics(currentMonth, currentYear);

                // 1.4. Thống Kê Đồ Ăn & Combo
                model.FoodStatistics = GetFoodStatistics(currentMonth, currentYear);

                // 1.5. Thống Kê Khách Hàng
                model.CustomerStatistics = GetCustomerStatistics(currentMonth, currentYear);

                // 1.6. Thống Kê Nhân Viên
                model.StaffStatistics = GetStaffStatistics(currentMonth, currentYear);

                // Dữ liệu cho biểu đồ
                ViewBag.ChartData = GetRevenueChartData(currentMonth, currentYear);

                return View(model);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thực hiện thống kê.";
                return View(new AdminDashboardViewModel());
            }
        }

        #region 1.1. Dashboard Tổng Hợp

        private DashboardSummaryViewModel GetDashboardSummary(DateTime today, int? month, int year)
        {
            var summary = new DashboardSummaryViewModel();

            // Chỉ lấy đơn đã thanh toán
            var paidBookings = db.Dat_Ves.Where(d => d.trang_thai_Dat_Ve == "Đã Thanh toán");

            // --- HÔM NAY ---
            var todayBookings = paidBookings.Where(d => 
                d.Ves.Any(v => v.Suat_Chieu != null && v.Suat_Chieu.ngay_chieu.Date == today));

            summary.TongVeBanHomNay = todayBookings.SelectMany(d => d.Ves).Count();
            summary.TongDoanhThuHomNay = todayBookings.AsEnumerable().Sum(d =>
                (decimal)(d.Ves.Sum(v => (decimal?)v.gia_ve) ?? 0) +
                (decimal)(d.DonHang_DoAns.Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0));
            summary.TongDoanhThuComboHomNay = todayBookings.AsEnumerable()
                .Sum(d => (decimal)(d.DonHang_DoAns.Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0));
            summary.SoSuatChieuHomNay = db.Suat_Chieus.Count(s => s.ngay_chieu.Date == today);
            summary.SoKhachMoiDangKyHomNay = db.Khach_Hangs.Count(k => k.ngay_dang_ky.HasValue && k.ngay_dang_ky.Value.Date == today);

            // Vé hủy hôm nay
            var cancelledToday = db.Dat_Ves.Where(d => 
                d.trang_thai_Dat_Ve == "Đã Hủy" && 
                d.ngay_tao.HasValue && 
                d.ngay_tao.Value.Date == today);
            summary.TongVeHuyHomNay = cancelledToday.SelectMany(d => d.Ves).Count();

            // --- TỔNG HỆ THỐNG (theo filter) ---
            var filteredBookings = paidBookings;
            if (month.HasValue)
            {
                filteredBookings = filteredBookings.Where(d =>
                    d.Ves.Any(v => v.Suat_Chieu != null && 
                        v.Suat_Chieu.ngay_chieu.Year == year && 
                        v.Suat_Chieu.ngay_chieu.Month == month.Value));
            }
            else
            {
                filteredBookings = filteredBookings.Where(d =>
                    d.Ves.Any(v => v.Suat_Chieu != null && v.Suat_Chieu.ngay_chieu.Year == year));
            }

            summary.TongVeBanHeThong = filteredBookings.SelectMany(d => d.Ves).Count();
            summary.TongDoanhThuHeThong = filteredBookings.AsEnumerable().Sum(d =>
                (decimal)(d.Ves.Sum(v => (decimal?)v.gia_ve) ?? 0) +
                (decimal)(d.DonHang_DoAns.Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0));

            summary.TongKhachHang = db.Khach_Hangs.Count();
            summary.TongPhim = db.Phims.Count();
            summary.TongRap = db.Raps.Count();

            return summary;
        }

        #endregion

        #region 1.2. Thống Kê Theo Rạp

        private List<CinemaStatisticViewModel> GetCinemaStatistics(int? month, int year)
        {
            var paidBookings = db.Dat_Ves.Where(d => d.trang_thai_Dat_Ve == "Đã Thanh toán");

            // Filter theo tháng/năm
            if (month.HasValue)
            {
                paidBookings = paidBookings.Where(d =>
                    d.Ves.Any(v => v.Suat_Chieu != null &&
                        v.Suat_Chieu.ngay_chieu.Year == year &&
                        v.Suat_Chieu.ngay_chieu.Month == month.Value));
            }
            else
            {
                paidBookings = paidBookings.Where(d =>
                    d.Ves.Any(v => v.Suat_Chieu != null && v.Suat_Chieu.ngay_chieu.Year == year));
            }

            var cinemaStats = new List<CinemaStatisticViewModel>();

            foreach (var rap in db.Raps.ToList())
            {
                var rapBookings = paidBookings.Where(d => 
                    d.Ves.Any(v => v.Suat_Chieu != null && 
                        v.Suat_Chieu.Phong_Chieu != null && 
                        v.Suat_Chieu.Phong_Chieu.rap_id == rap.rap_id));

                var tickets = rapBookings.SelectMany(d => d.Ves).ToList();
                var doanhThuVe = tickets.Sum(v => (decimal?)v.gia_ve) ?? 0m;
                var doanhThuDoAn = rapBookings.AsEnumerable()
                    .Sum(d => (decimal)(d.DonHang_DoAns.Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0));

                // Tổng số ghế trong rạp - sử dụng suc_chua
                var tongSoGhe = db.Phong_Chieus.Where(p => p.rap_id == rap.rap_id)
                    .Sum(p => (int?)p.suc_chua) ?? 0;

                // Suất chiếu
                var suatChieuQuery = db.Suat_Chieus.Where(s => s.Phong_Chieu.rap_id == rap.rap_id);
                if (month.HasValue)
                {
                    suatChieuQuery = suatChieuQuery.Where(s => 
                        s.ngay_chieu.Year == year && s.ngay_chieu.Month == month.Value);
                }
                else
                {
                    suatChieuQuery = suatChieuQuery.Where(s => s.ngay_chieu.Year == year);
                }
                var soSuatChieu = suatChieuQuery.Count();

                // Vé hủy
                var cancelledBookings = db.Dat_Ves.Where(d => 
                    d.trang_thai_Dat_Ve == "Đã Hủy" &&
                    d.Ves.Any(v => v.Suat_Chieu != null && 
                        v.Suat_Chieu.Phong_Chieu != null && 
                        v.Suat_Chieu.Phong_Chieu.rap_id == rap.rap_id));
                
                if (month.HasValue)
                {
                    cancelledBookings = cancelledBookings.Where(d =>
                        d.Ves.Any(v => v.Suat_Chieu.ngay_chieu.Year == year && 
                            v.Suat_Chieu.ngay_chieu.Month == month.Value));
                }
                else
                {
                    cancelledBookings = cancelledBookings.Where(d =>
                        d.Ves.Any(v => v.Suat_Chieu.ngay_chieu.Year == year));
                }

                var soVeHuy = cancelledBookings.SelectMany(d => d.Ves).Count();

                var stat = new CinemaStatisticViewModel
                {
                    RapId = rap.rap_id,
                    TenRap = rap.ten_rap,
                    DiaChi = rap.dia_chi,
                    DoanhThuVe = doanhThuVe,
                    DoanhThuDoAn = doanhThuDoAn,
                    TongDoanhThu = doanhThuVe + doanhThuDoAn,
                    SoVeBan = tickets.Count,
                    SoVeHuy = soVeHuy,
                    TongSoGhe = tongSoGhe,
                    SoGheDaBan = tickets.Count,
                    TyLeLapDay = tongSoGhe > 0 ? (decimal)tickets.Count / tongSoGhe * 100 : 0,
                    SoSuatChieu = soSuatChieu
                };

                cinemaStats.Add(stat);
            }

            // Xếp hạng
            var sortedByRevenue = cinemaStats.OrderByDescending(c => c.TongDoanhThu).ToList();
            var sortedByTickets = cinemaStats.OrderByDescending(c => c.SoVeBan).ToList();

            for (int i = 0; i < sortedByRevenue.Count; i++)
            {
                sortedByRevenue[i].XepHangDoanhThu = i + 1;
            }
            for (int i = 0; i < sortedByTickets.Count; i++)
            {
                sortedByTickets[i].XepHangSoVe = i + 1;
            }

            return cinemaStats.OrderByDescending(c => c.TongDoanhThu).ToList();
        }

        #endregion

        #region 1.3. Thống Kê Theo Phim

        private MovieStatisticsViewModel GetMovieStatistics(int? month, int year)
        {
            var stats = new MovieStatisticsViewModel();
            var paidBookings = db.Dat_Ves.Where(d => d.trang_thai_Dat_Ve == "Đã Thanh toán");

            // Filter
            if (month.HasValue)
            {
                paidBookings = paidBookings.Where(d =>
                    d.Ves.Any(v => v.Suat_Chieu != null &&
                        v.Suat_Chieu.ngay_chieu.Year == year &&
                        v.Suat_Chieu.ngay_chieu.Month == month.Value));
            }
            else
            {
                paidBookings = paidBookings.Where(d =>
                    d.Ves.Any(v => v.Suat_Chieu != null && v.Suat_Chieu.ngay_chieu.Year == year));
            }

            // Top phim theo doanh thu
            var movieRevenues = paidBookings.SelectMany(d => d.Ves)
                .Where(v => v.Suat_Chieu != null && v.Suat_Chieu.Phim != null)
                .GroupBy(v => v.Suat_Chieu.phim_id)
                .Select(g => new
                {
                    PhimId = g.Key,
                    SoVe = g.Count(),
                    DoanhThuVe = g.Sum(v => (decimal?)v.gia_ve) ?? 0m
                })
                .AsEnumerable()
                .Select(x => new MovieRevenueViewModel
                {
                    PhimId = x.PhimId,
                    SoVeBan = x.SoVe,
                    DoanhThuVe = x.DoanhThuVe,
                    DoanhThuDoAn = paidBookings
                        .Where(d => d.Ves.Any(v => v.Suat_Chieu.phim_id == x.PhimId))
                        .AsEnumerable()
                        .Sum(d => (decimal)(d.DonHang_DoAns.Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0))
                })
                .ToList();

            // Gán thông tin phim
            foreach (var item in movieRevenues)
            {
                var phim = db.Phims.FirstOrDefault(p => p.phim_id == item.PhimId);
                if (phim != null)
                {
                    item.TenPhim = phim.ten_phim;
                    item.AnhBia = phim.hinh_anh; // Sử dụng hinh_anh thay vì anh_bia
                    item.TongDoanhThu = item.DoanhThuVe + item.DoanhThuDoAn;

                    // Số suất chiếu
                    var suatChieuQuery = db.Suat_Chieus.Where(s => s.phim_id == item.PhimId);
                    if (month.HasValue)
                    {
                        suatChieuQuery = suatChieuQuery.Where(s => 
                            s.ngay_chieu.Year == year && s.ngay_chieu.Month == month.Value);
                    }
                    else
                    {
                        suatChieuQuery = suatChieuQuery.Where(s => s.ngay_chieu.Year == year);
                    }
                    item.SoSuatChieu = suatChieuQuery.Count();
                }
            }

            // Loại bỏ phim null
            movieRevenues = movieRevenues.Where(m => !string.IsNullOrEmpty(m.TenPhim)).ToList();

            stats.TopPhimDoanhThuCao = movieRevenues.OrderByDescending(m => m.TongDoanhThu).Take(10).ToList();
            stats.TopPhimBanChay = movieRevenues.OrderByDescending(m => m.SoVeBan).Take(10).ToList();
            stats.PhimBiE = movieRevenues.OrderBy(m => m.TongDoanhThu).Take(5).ToList();

            // Suất chiếu tốt nhất
            stats.SuatChieuTotNhat = GetTopShowtimes(month, year);

            // So sánh phim theo rạp
            stats.SoSanhPhimTheoRap = GetMovieCinemaComparison(month, year);

            return stats;
        }

        private List<ShowtimePerformanceViewModel> GetTopShowtimes(int? month, int year)
        {
            var paidBookings = db.Dat_Ves.Where(d => d.trang_thai_Dat_Ve == "Đã Thanh toán");

            var showtimeStats = paidBookings.SelectMany(d => d.Ves)
                .Where(v => v.Suat_Chieu != null)
                .GroupBy(v => v.suat_chieu_id)
                .Select(g => new
                {
                    SuatChieuId = g.Key,
                    SoVe = g.Count(),
                    DoanhThu = g.Sum(v => (decimal?)v.gia_ve) ?? 0m
                })
                .ToList()
                .Select(x =>
                {
                    var sc = db.Suat_Chieus.FirstOrDefault(s => s.suat_chieu_id == x.SuatChieuId);
                    if (sc == null) return null;

                    // Filter theo tháng/năm
                    if (month.HasValue && (sc.ngay_chieu.Year != year || sc.ngay_chieu.Month != month.Value))
                        return null;
                    if (!month.HasValue && sc.ngay_chieu.Year != year)
                        return null;

                    var tongGhe = sc.Phong_Chieu.suc_chua; // Sử dụng suc_chua
                    return new ShowtimePerformanceViewModel
                    {
                        SuatChieuId = sc.suat_chieu_id,
                        TenPhim = sc.Phim?.ten_phim,
                        TenRap = sc.Phong_Chieu?.Rap?.ten_rap,
                        TenPhong = sc.Phong_Chieu?.ten_phong,
                        NgayChieu = sc.ngay_chieu,
                        GioChieu = sc.Ca_Chieu.gio_bat_dau, // Sử dụng Ca_Chieu.gio_bat_dau thay vì gio_chieu
                        SoVeBan = x.SoVe,
                        TongSoGhe = tongGhe,
                        TyLeLapDay = tongGhe > 0 ? (decimal)x.SoVe / tongGhe * 100 : 0,
                        DoanhThu = x.DoanhThu
                    };
                })
                .Where(x => x != null)
                .OrderByDescending(x => x.TyLeLapDay)
                .Take(10)
                .ToList();

            return showtimeStats;
        }

        private List<MovieCinemaComparisonViewModel> GetMovieCinemaComparison(int? month, int year)
        {
            var paidBookings = db.Dat_Ves.Where(d => d.trang_thai_Dat_Ve == "Đã Thanh toán");

            // Filter
            if (month.HasValue)
            {
                paidBookings = paidBookings.Where(d =>
                    d.Ves.Any(v => v.Suat_Chieu != null &&
                        v.Suat_Chieu.ngay_chieu.Year == year &&
                        v.Suat_Chieu.ngay_chieu.Month == month.Value));
            }
            else
            {
                paidBookings = paidBookings.Where(d =>
                    d.Ves.Any(v => v.Suat_Chieu != null && v.Suat_Chieu.ngay_chieu.Year == year));
            }

            // Lấy top 5 phim bán chạy
            var topMovies = paidBookings.SelectMany(d => d.Ves)
                .Where(v => v.Suat_Chieu != null && v.Suat_Chieu.Phim != null)
                .GroupBy(v => v.Suat_Chieu.phim_id)
                .Select(g => new { PhimId = g.Key, SoVe = g.Count() })
                .OrderByDescending(x => x.SoVe)
                .Take(5)
                .ToList();

            var result = new List<MovieCinemaComparisonViewModel>();

            foreach (var movie in topMovies)
            {
                var phim = db.Phims.FirstOrDefault(p => p.phim_id == movie.PhimId);
                if (phim == null) continue;

                var chiTiet = db.Raps.ToList().Select(rap =>
                {
                    var rapBookings = paidBookings.Where(d =>
                        d.Ves.Any(v => v.Suat_Chieu.phim_id == movie.PhimId &&
                            v.Suat_Chieu.Phong_Chieu.rap_id == rap.rap_id));

                    var tickets = rapBookings.SelectMany(d => d.Ves).Count();
                    var doanhThu = rapBookings.AsEnumerable().Sum(d =>
                        (decimal)(d.Ves.Where(v => v.Suat_Chieu.phim_id == movie.PhimId).Sum(v => (decimal?)v.gia_ve) ?? 0) +
                        (decimal)(d.DonHang_DoAns.Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0));

                    var suatChieu = db.Suat_Chieus.Count(s => 
                        s.phim_id == movie.PhimId && s.Phong_Chieu.rap_id == rap.rap_id);

                    return new CinemaRevenueDetail
                    {
                        RapId = rap.rap_id,
                        TenRap = rap.ten_rap,
                        SoVeBan = tickets,
                        DoanhThu = doanhThu,
                        SoSuatChieu = suatChieu
                    };
                }).Where(x => x.SoVeBan > 0).ToList();

                result.Add(new MovieCinemaComparisonViewModel
                {
                    PhimId = movie.PhimId,
                    TenPhim = phim.ten_phim,
                    ChiTietTheoRap = chiTiet
                });
            }

            return result;
        }

        #endregion

        #region 1.4. Thống Kê Đồ Ăn & Combo

        private FoodStatisticsViewModel GetFoodStatistics(int? month, int year)
        {
            var stats = new FoodStatisticsViewModel();
            var paidBookings = db.Dat_Ves.Where(d => d.trang_thai_Dat_Ve == "Đã Thanh toán");

            // Filter
            if (month.HasValue)
            {
                paidBookings = paidBookings.Where(d =>
                    d.Ves.Any(v => v.Suat_Chieu != null &&
                        v.Suat_Chieu.ngay_chieu.Year == year &&
                        v.Suat_Chieu.ngay_chieu.Month == month.Value));
            }
            else
            {
                paidBookings = paidBookings.Where(d =>
                    d.Ves.Any(v => v.Suat_Chieu != null && v.Suat_Chieu.ngay_chieu.Year == year));
            }

            // Món bán chạy nhất
            var foodStats = paidBookings.SelectMany(d => d.DonHang_DoAns)
                .Where(dda => dda.Do_An != null)
                .GroupBy(dda => dda.Do_An_id)
                .Select(g => new FoodItemStatViewModel
                {
                    DoAnId = g.Key,
                    SoLuongBan = g.Sum(dda => dda.so_luong),
                    TongDoanhThu = g.Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0m
                })
                .ToList();

            foreach (var item in foodStats)
            {
                var doAn = db.Do_Ans.FirstOrDefault(d => d.Do_An_id == item.DoAnId);
                if (doAn != null)
                {
                    item.TenSanPham = doAn.ten_san_pham;
                    // Kiểm tra property hinh_anh có tồn tại không
                    try 
                    { 
                        var hinhAnhProp = doAn.GetType().GetProperty("hinh_anh");
                        item.HinhAnh = hinhAnhProp?.GetValue(doAn) as string;
                    } 
                    catch { item.HinhAnh = null; }
                    
                    item.Gia = doAn.gia ?? 0;
                    item.LoaiDoAn = doAn.loai; // Sử dụng loai thay vì loai_do_an
                }
            }

            stats.MonBanChayNhat = foodStats.Where(f => !string.IsNullOrEmpty(f.TenSanPham))
                .OrderByDescending(f => f.SoLuongBan).Take(10).ToList();

            // Doanh thu đồ ăn theo rạp
            stats.DoanhThuDoAnTheoRap = GetFoodRevenueByCinema(paidBookings);

            // Tỷ lệ khách mua combo
            stats.TongKhachMuaVe = paidBookings.Count();
            stats.SoKhachMuaCombo = paidBookings.Count(d => d.DonHang_DoAns.Any());
            stats.TyLeKhachMuaCombo = stats.TongKhachMuaVe > 0 
                ? (decimal)stats.SoKhachMuaCombo / stats.TongKhachMuaVe * 100 : 0;

            // Tổng tiền giảm qua khuyến mãi - tính từ Khuyen_Mai
            var promotionUsage = paidBookings.Where(d => d.ma_giam_gia_id.HasValue).ToList();
            stats.SoLuotSuDungKhuyenMai = promotionUsage.Count;
            // Tính tổng tiền giảm dựa trên giá trị khuyến mãi
            stats.TongTienGiamKhuyenMai = promotionUsage.Sum(d => 
            {
                var promo = db.Khuyen_Mais.FirstOrDefault(k => k.ma_giam_gia_id == d.ma_giam_gia_id);
                if (promo == null) return 0m;
                
                bool isPercent = !string.IsNullOrEmpty(promo.loai_giam_gia) && 
                    (promo.loai_giam_gia.Contains("%") || promo.loai_giam_gia.IndexOf("Phần", StringComparison.OrdinalIgnoreCase) >= 0);
                
                if (isPercent)
                {
                    var pct = promo.gia_tri_giam;
                    if (pct < 0) pct = 0;
                    if (pct > 100) pct = 100;
                    return (d.tong_tien / (1 - pct / 100m)) * (pct / 100m);
                }
                return promo.gia_tri_giam;
            });

            return stats;
        }

        private List<FoodRevenueByCinemaViewModel> GetFoodRevenueByCinema(IQueryable<Dat_Ve> paidBookings)
        {
            var result = new List<FoodRevenueByCinemaViewModel>();

            foreach (var rap in db.Raps.ToList())
            {
                var rapBookings = paidBookings.Where(d =>
                    d.Ves.Any(v => v.Suat_Chieu != null &&
                        v.Suat_Chieu.Phong_Chieu != null &&
                        v.Suat_Chieu.Phong_Chieu.rap_id == rap.rap_id));

                var foodItems = rapBookings.SelectMany(d => d.DonHang_DoAns)
                    .Where(dda => dda.Do_An != null)
                    .GroupBy(dda => dda.Do_An_id)
                    .Select(g => new FoodItemStatViewModel
                    {
                        DoAnId = g.Key,
                        SoLuongBan = g.Sum(dda => dda.so_luong),
                        TongDoanhThu = g.Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0m
                    })
                    .ToList();

                foreach (var item in foodItems)
                {
                    var doAn = db.Do_Ans.FirstOrDefault(d => d.Do_An_id == item.DoAnId);
                    if (doAn != null)
                    {
                        item.TenSanPham = doAn.ten_san_pham;
                        item.Gia = doAn.gia ?? 0;
                    }
                }

                if (foodItems.Any())
                {
                    result.Add(new FoodRevenueByCinemaViewModel
                    {
                        RapId = rap.rap_id,
                        TenRap = rap.ten_rap,
                        TongDoanhThuDoAn = foodItems.Sum(f => f.TongDoanhThu),
                        SoLuongMonBan = foodItems.Sum(f => f.SoLuongBan),
                        TopMonBanChay = foodItems.OrderByDescending(f => f.SoLuongBan).Take(3).ToList()
                    });
                }
            }

            return result.OrderByDescending(r => r.TongDoanhThuDoAn).ToList();
        }

        #endregion

        #region 1.5. Thống Kê Khách Hàng

        private CustomerStatisticsViewModel GetCustomerStatistics(int? month, int year)
        {
            var stats = new CustomerStatisticsViewModel();

            stats.TongKhachDangKy = db.Khach_Hangs.Count();

            // Khách mới trong tháng - sử dụng ngay_dang_ky
            var currentMonth = month ?? DateTime.Now.Month;
            stats.KhachMoiTrongThang = db.Khach_Hangs.Count(k =>
                k.ngay_dang_ky.HasValue &&
                k.ngay_dang_ky.Value.Year == year &&
                k.ngay_dang_ky.Value.Month == currentMonth);

            // Khách VIP (điểm > 100)
            stats.KhachVIP = db.Khach_Hangs.Count(k => (k.diem_tich_luy ?? 0) >= 100);

            // Top khách mua nhiều
            var paidBookings = db.Dat_Ves.Where(d => d.trang_thai_Dat_Ve == "Đã Thanh toán");

            var customerStats = paidBookings
                .GroupBy(d => d.khach_hang_id)
                .Select(g => new
                {
                    KhachHangId = g.Key,
                    SoDon = g.Count(),
                    SoVe = g.SelectMany(d => d.Ves).Count(),
                    TongTien = g.AsEnumerable().Sum(d =>
                        (decimal)(d.Ves.Sum(v => (decimal?)v.gia_ve) ?? 0) +
                        (decimal)(d.DonHang_DoAns.Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0))
                })
                .OrderByDescending(x => x.TongTien)
                .Take(10)
                .ToList();

            stats.TopKhachMuaNhieu = customerStats.Select(x =>
            {
                var kh = db.Khach_Hangs.FirstOrDefault(k => k.khach_hang_id == x.KhachHangId);
                return kh != null ? new TopCustomerViewModel
                {
                    KhachHangId = kh.khach_hang_id,
                    HoTen = kh.ho_ten,
                    Email = kh.email,
                    SoDienThoai = kh.so_dien_thoai,
                    SoDonDat = x.SoDon,
                    SoVeMua = x.SoVe,
                    TongChiTieu = x.TongTien,
                    DiemTichLuy = kh.diem_tich_luy ?? 0
                } : null;
            }).Where(x => x != null).ToList();

            // Số tiền trung bình
            var totalOrders = paidBookings.Count();
            if (totalOrders > 0)
            {
                var totalRevenue = paidBookings.AsEnumerable().Sum(d =>
                    (decimal)(d.Ves.Sum(v => (decimal?)v.gia_ve) ?? 0) +
                    (decimal)(d.DonHang_DoAns.Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0));
                stats.SoTienTrungBinhMoiDon = totalRevenue / totalOrders;

                var totalTickets = paidBookings.SelectMany(d => d.Ves).Count();
                var totalCustomers = paidBookings.Select(d => d.khach_hang_id).Distinct().Count();
                stats.SoVeTrungBinhMoiKhach = totalCustomers > 0 ? (decimal)totalTickets / totalCustomers : 0;
            }

            return stats;
        }

        #endregion

        #region 1.6. Thống Kê Nhân Viên

        private StaffStatisticsViewModel GetStaffStatistics(int? month, int year)
        {
            var stats = new StaffStatisticsViewModel();

            stats.TongNhanVien = db.Nhan_Viens.Count();
            stats.NhanVienDangLamViec = db.Nhan_Viens.Count(n => n.trang_thai == "Đang làm việc");

            // Hoạt động nhân viên (chỉ lấy đơn do nhân viên tạo)
            var staffBookings = db.Dat_Ves.Where(d => 
                d.nhan_vien_id.HasValue && 
                d.trang_thai_Dat_Ve == "Đã Thanh toán");

            // Filter
            if (month.HasValue)
            {
                staffBookings = staffBookings.Where(d =>
                    d.ngay_tao.HasValue &&
                    d.ngay_tao.Value.Year == year &&
                    d.ngay_tao.Value.Month == month.Value);
            }
            else
            {
                staffBookings = staffBookings.Where(d =>
                    d.ngay_tao.HasValue && d.ngay_tao.Value.Year == year);
            }

            var staffActivities = staffBookings
                .GroupBy(d => d.nhan_vien_id)
                .Select(g => new
                {
                    NhanVienId = g.Key.Value,
                    SoDon = g.Count(),
                    SoVe = g.SelectMany(d => d.Ves).Count(),
                    DoanhThu = g.AsEnumerable().Sum(d =>
                        (decimal)(d.Ves.Sum(v => (decimal?)v.gia_ve) ?? 0) +
                        (decimal)(d.DonHang_DoAns.Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0))
                })
                .OrderByDescending(x => x.DoanhThu)
                .ToList();

            stats.HoatDongNhanVien = staffActivities.Select(x =>
            {
                var nv = db.Nhan_Viens.FirstOrDefault(n => n.nhanvien_id == x.NhanVienId);
                if (nv == null) return null;

                // Đếm giao dịch thất bại (Đã Hủy)
                var failedCount = db.Dat_Ves.Count(d =>
                    d.nhan_vien_id == x.NhanVienId &&
                    d.trang_thai_Dat_Ve == "Đã Hủy");

                return new StaffActivityViewModel
                {
                    NhanVienId = nv.nhanvien_id,
                    HoTen = nv.ho_ten,
                    TenRap = nv.Rap?.ten_rap,
                    ChucVu = nv.Role?.ten_role, // Sử dụng Role.ten_role thay vì chuc_vu
                    SoVeBan = x.SoVe,
                    DoanhThuBanVe = x.DoanhThu,
                    SoGiaoDichThanhCong = x.SoDon,
                    SoGiaoDichThatBai = failedCount,
                    SoKhachDuocHoTro = x.SoDon, // Giả định mỗi đơn = 1 khách
                    //SoLoiThaoTac = 0 // Cần tracking riêng nếu có
                };
            }).Where(x => x != null).ToList();

            return stats;
        }

        #endregion

        #region Biểu Đồ

        private RevenueChartDataViewModel GetRevenueChartData(int? month, int year)
        {
            var chartData = new RevenueChartDataViewModel
            {
                Labels = new List<string>(),
                DoanhThuVe = new List<decimal>(),
                DoanhThuDoAn = new List<decimal>(),
                TongDoanhThu = new List<decimal>()
            };

            var paidBookings = db.Dat_Ves.Where(d => d.trang_thai_Dat_Ve == "Đã Thanh toán");

            if (month.HasValue)
            {
                // Theo ngày trong tháng
                int daysInMonth = DateTime.DaysInMonth(year, month.Value);
                for (int day = 1; day <= daysInMonth; day++)
                {
                    var date = new DateTime(year, month.Value, day);
                    var dayBookings = paidBookings.Where(d =>
                        d.Ves.Any(v => v.Suat_Chieu != null && v.Suat_Chieu.ngay_chieu.Date == date));

                    var doanhThuVe = dayBookings.SelectMany(d => d.Ves).Sum(v => (decimal?)v.gia_ve) ?? 0m;
                    var doanhThuDoAn = dayBookings.AsEnumerable()
                        .Sum(d => (decimal)(d.DonHang_DoAns.Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0));

                    chartData.Labels.Add($"{day}/{month}");
                    chartData.DoanhThuVe.Add(doanhThuVe);
                    chartData.DoanhThuDoAn.Add(doanhThuDoAn);
                    chartData.TongDoanhThu.Add(doanhThuVe + doanhThuDoAn);
                }
            }
            else
            {
                // Theo tháng trong năm
                for (int m = 1; m <= 12; m++)
                {
                    var monthBookings = paidBookings.Where(d =>
                        d.Ves.Any(v => v.Suat_Chieu != null &&
                            v.Suat_Chieu.ngay_chieu.Year == year &&
                            v.Suat_Chieu.ngay_chieu.Month == m));

                    var doanhThuVe = monthBookings.SelectMany(d => d.Ves).Sum(v => (decimal?)v.gia_ve) ?? 0m;
                    var doanhThuDoAn = monthBookings.AsEnumerable()
                        .Sum(d => (decimal)(d.DonHang_DoAns.Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0));

                    chartData.Labels.Add($"T{m}");
                    chartData.DoanhThuVe.Add(doanhThuVe);
                    chartData.DoanhThuDoAn.Add(doanhThuDoAn);
                    chartData.TongDoanhThu.Add(doanhThuVe + doanhThuDoAn);
                }
            }

            return chartData;
        }

        #endregion

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
