using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;
using Newtonsoft.Json;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Staff")]
    public class StaffDashboardNewController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/StaffDashboardNew
        public ActionResult Index(int? filterMonth, int? filterYear)
        {
            try
            {
                // ✅ Lấy thông tin Staff hiện tại
                int? employeeId = Session["EmployeeId"] as int?;
                if (!employeeId.HasValue)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin nhân viên. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                var staff = db.Nhan_Viens.FirstOrDefault(nv => nv.nhanvien_id == employeeId.Value);
                if (staff == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin nhân viên.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                // ✅ KIỂM TRA QUYỀN: Staff PHẢI có rap_id
                if (!staff.rap_id.HasValue)
                {
                    TempData["ErrorMessage"] = "Bạn chưa được gán rạp. Vui lòng liên hệ quản trị viên.";
                    return RedirectToAction("Index", "Home", new { area = "" });
                }

                int rapId = staff.rap_id.Value;
                var rap = staff.Rap;

                // Filter
                int currentYear = filterYear ?? DateTime.Now.Year;
                int? currentMonth = filterMonth;

                var model = new StaffDashboardViewModel
                {
                    RapId = rapId,
                    TenRap = rap.ten_rap,
                    DiaChiRap = rap.dia_chi,
                    FilterMonth = currentMonth,
                    FilterYear = currentYear
                };

                // ✅ 2.1 DASHBOARD CHO STAFF
                model.Summary = GetStaffSummary(rapId, currentMonth, currentYear);

                // ✅ 2.2 THỐNG KÊ PHÒNG CHIẾU
                model.RoomStatistics = GetRoomStatistics(rapId, currentMonth, currentYear);

                // ✅ 2.3 THỐNG KÊ PHIM TẠI RẠP
                model.MovieStatistics = GetMovieStatistics(rapId, currentMonth, currentYear);

                // ✅ 2.4 COMBO - ĐỒ ĂN
                model.FoodStatistics = GetFoodStatistics(rapId, currentMonth, currentYear);

                // ✅ 2.5 THỐNG KÊ NHÂN VIÊN CẤP RẠP
                model.EmployeeStatistics = GetEmployeeStatistics(rapId, currentMonth, currentYear);

                // ✅ BIỂU ĐỒ DOANH THU
                model.ChartData = GetChartData(rapId, currentMonth, currentYear);
                ViewBag.ChartData = model.ChartData;

                return View(model);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải Dashboard: " + ex.Message;
                return View(new StaffDashboardViewModel());
            }
        }

        #region 2.1 Staff Summary

        private StaffSummaryViewModel GetStaffSummary(int rapId, int? month, int year)
        {
            var today = DateTime.Today;

            // ✅ HÔM NAY - THEO NGÀY SUẤT CHIẾU
            var todayTickets = db.Ves
                .Where(v => v.Dat_Ve != null 
                    && v.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán"
                    && v.Suat_Chieu.ngay_chieu == today
                    && v.Suat_Chieu.Phong_Chieu.rap_id == rapId)
                .ToList();

            var todayDatVeIds = todayTickets.Select(v => v.Dat_Ve_id).Distinct().ToList();

            var summary = new StaffSummaryViewModel
            {
                // Hôm nay - theo suất chiếu
                TongDoanhThuHomNay = todayTickets.Sum(v => (decimal?)v.gia_ve) ?? 0,
                
                TongVeBanHomNay = todayTickets.Count,

                TongVeHuyHomNay = db.Ves
                    .Where(v => v.trang_thai_ve == "Đã Hủy" 
                        && v.Suat_Chieu.ngay_chieu == today
                        && v.Suat_Chieu.Phong_Chieu.rap_id == rapId)
                    .Count(),

                SoSuatChieuHomNay = db.Suat_Chieus
                    .Count(s => s.Phong_Chieu.rap_id == rapId && s.ngay_chieu == today),

                TongDoanhThuComboHomNay = db.DonHang_DoAns
                    .Where(dda => todayDatVeIds.Contains(dda.Dat_Ve_id))
                    .Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0,

                // Tổng quan rạp
                TongPhongChieu = db.Phong_Chieus.Count(p => p.rap_id == rapId),
                TongGhe = db.Phong_Chieus.Where(p => p.rap_id == rapId).Sum(p => (int?)p.Ghes.Count) ?? 0,
                TongNhanVien = db.Nhan_Viens.Count(nv => nv.rap_id == rapId && nv.trang_thai == "Đang làm việc")
            };

            // Cộng thêm doanh thu đồ ăn vào tổng doanh thu hôm nay
            summary.TongDoanhThuHomNay += summary.TongDoanhThuComboHomNay;

            // ✅ THEO FILTER - THEO NGÀY SUẤT CHIẾU
            var filterTicketsQuery = db.Ves
                .Where(v => v.Dat_Ve != null 
                    && v.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán"
                    && v.Suat_Chieu.Phong_Chieu.rap_id == rapId);

            if (month.HasValue)
            {
                filterTicketsQuery = filterTicketsQuery.Where(v => 
                    v.Suat_Chieu.ngay_chieu.Year == year && 
                    v.Suat_Chieu.ngay_chieu.Month == month.Value);
            }
            else
            {
                filterTicketsQuery = filterTicketsQuery.Where(v => 
                    v.Suat_Chieu.ngay_chieu.Year == year);
            }

            var filterTickets = filterTicketsQuery.ToList();
            var filterDatVeIds = filterTickets.Select(v => v.Dat_Ve_id).Distinct().ToList();

            summary.TongDoanhThuTheoFilter = filterTickets.Sum(v => (decimal?)v.gia_ve) ?? 0;
            
            var filterFoodRevenue = db.DonHang_DoAns
                .Where(dda => filterDatVeIds.Contains(dda.Dat_Ve_id))
                .Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0;
            
            summary.TongDoanhThuTheoFilter += filterFoodRevenue;

            summary.TongVeBanTheoFilter = filterTickets.Count;

            return summary;
        }

        #endregion

        #region 2.2 Thống kê phòng chiếu

        private List<StaffRoomStatisticsViewModel> GetRoomStatistics(int rapId, int? month, int year)
        {
            var today = DateTime.Today;
            var rooms = db.Phong_Chieus.Where(p => p.rap_id == rapId).ToList();

            var result = new List<StaffRoomStatisticsViewModel>();

            foreach (var room in rooms)
            {
                // Suất chiếu hôm nay
                var todayShowtimes = db.Suat_Chieus
                    .Where(s => s.phong_chieu_id == room.phong_chieu_id && s.ngay_chieu == today)
                    .ToList();

                var roomStat = new StaffRoomStatisticsViewModel
                {
                    PhongId = room.phong_chieu_id,
                    TenPhong = room.ten_phong,
                    SoGhe = room.Ghes.Count,
                    SoSuatChieuHomNay = todayShowtimes.Count,

                    SuatChieuHomNay = todayShowtimes.Select(s => new StaffShowtimeInRoomViewModel
                    {
                        SuatChieuId = s.suat_chieu_id,
                        TenPhim = s.Phim?.ten_phim ?? "N/A",
                        GioBatDau = s.Ca_Chieu?.gio_bat_dau ?? TimeSpan.Zero,
                        SoVeBan = s.Ves.Count(v => v.Dat_Ve_id != null),
                        TongGhe = room.Ghes.Count,
                        TyLeLapDay = room.Ghes.Count > 0 ? (decimal)s.Ves.Count(v => v.Dat_Ve_id != null) / room.Ghes.Count * 100 : 0,
                        TrangThai = s.ngay_chieu.Add(s.Ca_Chieu?.gio_bat_dau ?? TimeSpan.Zero) > DateTime.Now ? "Sắp chiếu" : "Đã chiếu"
                    }).OrderBy(s => s.GioBatDau).ToList(),

                    TrangThaiThietBi = "Bình thường" // Có thể lấy từ bảng khác nếu có
                };

                // Tính tỷ lệ lấp đầy trung bình theo filter
                var filterShowtimes = db.Suat_Chieus.Where(s => s.phong_chieu_id == room.phong_chieu_id);
                if (month.HasValue)
                {
                    filterShowtimes = filterShowtimes.Where(s => s.ngay_chieu.Year == year && s.ngay_chieu.Month == month.Value);
                }
                else
                {
                    filterShowtimes = filterShowtimes.Where(s => s.ngay_chieu.Year == year);
                }

                var showtimesList = filterShowtimes.ToList();
                if (showtimesList.Any())
                {
                    var avgOccupancy = showtimesList.Average(s =>
                        room.Ghes.Count > 0 ? (decimal)s.Ves.Count(v => v.Dat_Ve_id != null) / room.Ghes.Count * 100 : 0);
                    roomStat.TyLeLapDayTrungBinh = avgOccupancy;

                    roomStat.TongVeBan = showtimesList.Sum(s => s.Ves.Count(v => v.Dat_Ve_id != null));
                    roomStat.DoanhThu = showtimesList.Sum(s => s.Ves.Where(v => v.Dat_Ve_id != null).Sum(v => (decimal?)v.gia_ve) ?? 0);
                }

                result.Add(roomStat);
            }

            return result.OrderBy(r => r.TenPhong).ToList();
        }

        #endregion

        #region 2.3 Thống kê phim tại rạp

        private StaffMovieStatisticsViewModel GetMovieStatistics(int rapId, int? month, int year)
        {
            var showtimesQuery = db.Suat_Chieus.Where(s => s.Phong_Chieu.rap_id == rapId);

            if (month.HasValue)
            {
                showtimesQuery = showtimesQuery.Where(s => s.ngay_chieu.Year == year && s.ngay_chieu.Month == month.Value);
            }
            else
            {
                showtimesQuery = showtimesQuery.Where(s => s.ngay_chieu.Year == year);
            }

            var showtimes = showtimesQuery.ToList();

            // Phim bán chạy
            var moviePerformance = showtimes
                .GroupBy(s => s.phim_id)
                .Select(g => new
                {
                    PhimId = g.Key,
                    SoVeBan = g.Sum(s => s.Ves.Count(v => v.Dat_Ve_id != null)),
                    DoanhThu = g.Sum(s => s.Ves.Where(v => v.Dat_Ve_id != null).Sum(v => (decimal?)v.gia_ve) ?? 0),
                    SoSuatChieu = g.Count(),
                    TongGhe = g.Sum(s => s.Phong_Chieu.Ghes.Count),
                    TongVe = g.Sum(s => s.Ves.Count(v => v.Dat_Ve_id != null))
                })
                .ToList();

            var topMovies = moviePerformance
                .OrderByDescending(m => m.SoVeBan)
                .Take(10)
                .Select(m =>
                {
                    var phim = db.Phims.FirstOrDefault(p => p.phim_id == m.PhimId);
                    return new StaffMoviePerformanceViewModel
                    {
                        PhimId = m.PhimId,
                        TenPhim = phim?.ten_phim ?? "N/A",
                        HinhAnh = null, // Không còn trường hinh_anh trong Phims
                        SoVeBan = m.SoVeBan,
                        DoanhThu = m.DoanhThu,
                        SoSuatChieu = m.SoSuatChieu,
                        TyLeLapDayTrungBinh = m.TongGhe > 0 ? (decimal)m.TongVe / m.TongGhe * 100 : 0,
                        DoanhThuTrungBinhMoiSuat = m.SoSuatChieu > 0 ? m.DoanhThu / m.SoSuatChieu : 0
                    };
                })
                .ToList();

            // Phim lỗ vé (tỷ lệ lấp đầy < 30%)
            var lowPerformanceMovies = moviePerformance
                .Where(m => m.TongGhe > 0 && ((decimal)m.TongVe / m.TongGhe * 100) < 30)
                .OrderBy(m => (decimal)m.TongVe / m.TongGhe)
                .Take(5)
                .Select(m =>
                {
                    var phim = db.Phims.FirstOrDefault(p => p.phim_id == m.PhimId);
                    return new StaffMoviePerformanceViewModel
                    {
                        PhimId = m.PhimId,
                        TenPhim = phim?.ten_phim ?? "N/A",
                        HinhAnh = null, // Không còn trường hinh_anh trong Phims
                        SoVeBan = m.SoVeBan,
                        DoanhThu = m.DoanhThu,
                        SoSuatChieu = m.SoSuatChieu,
                        TyLeLapDayTrungBinh = m.TongGhe > 0 ? (decimal)m.TongVe / m.TongGhe * 100 : 0,
                        DoanhThuTrungBinhMoiSuat = m.SoSuatChieu > 0 ? m.DoanhThu / m.SoSuatChieu : 0
                    };
                })
                .ToList();

            // Top suất chiếu đông khách
            var topShowtimes = showtimes
                .OrderByDescending(s => s.Ves.Count(v => v.Dat_Ve_id != null))
                .Take(10)
                .Select(s => new StaffTopShowtimeViewModel
                {
                    SuatChieuId = s.suat_chieu_id,
                    TenPhim = s.Phim?.ten_phim ?? "N/A",
                    TenPhong = s.Phong_Chieu?.ten_phong ?? "N/A",
                    NgayChieu = s.ngay_chieu,
                    GioChieu = s.Ca_Chieu?.gio_bat_dau ?? TimeSpan.Zero,
                    SoVeBan = s.Ves.Count(v => v.Dat_Ve_id != null),
                    TongGhe = s.Phong_Chieu?.Ghes.Count ?? 0,
                    TyLeLapDay = (s.Phong_Chieu?.Ghes.Count ?? 0) > 0 
                        ? (decimal)s.Ves.Count(v => v.Dat_Ve_id != null) / s.Phong_Chieu.Ghes.Count * 100 
                        : 0,
                    DoanhThu = s.Ves.Where(v => v.Dat_Ve_id != null).Sum(v => (decimal?)v.gia_ve) ?? 0
                })
                .ToList();

            // Dự đoán suất tối ưu
            var recommendations = GenerateShowtimeRecommendations(showtimes);

            return new StaffMovieStatisticsViewModel
            {
                PhimBanChayNhat = topMovies,
                PhimLoVe = lowPerformanceMovies,
                SuatChieuDongKhachNhat = topShowtimes,
                DuDoanSuatToiUu = recommendations
            };
        }

        private List<StaffShowtimeRecommendationViewModel> GenerateShowtimeRecommendations(List<Suat_Chieu> showtimes)
        {
            var recommendations = new List<StaffShowtimeRecommendationViewModel>();

            // Phân tích theo khung giờ
            var morningShowtimes = showtimes.Where(s => s.Ca_Chieu != null && s.Ca_Chieu.gio_bat_dau.Hours < 12).ToList();
            var afternoonShowtimes = showtimes.Where(s => s.Ca_Chieu != null && s.Ca_Chieu.gio_bat_dau.Hours >= 12 && s.Ca_Chieu.gio_bat_dau.Hours < 18).ToList();
            var eveningShowtimes = showtimes.Where(s => s.Ca_Chieu != null && s.Ca_Chieu.gio_bat_dau.Hours >= 18 && s.Ca_Chieu.gio_bat_dau.Hours < 22).ToList();

            // Phân tích khung sáng
            if (morningShowtimes.Any())
            {
                var avgOccupancy = morningShowtimes.Average(s =>
                    (s.Phong_Chieu?.Ghes.Count ?? 0) > 0
                        ? (decimal)s.Ves.Count(v => v.Dat_Ve_id != null) / s.Phong_Chieu.Ghes.Count * 100
                        : 0);

                if (avgOccupancy < 30)
                {
                    recommendations.Add(new StaffShowtimeRecommendationViewModel
                    {
                        KhungGioGoiY = "Sáng",
                        LyDo = $"Tỷ lệ lấp đầy trung bình thấp ({avgOccupancy:F1}%)",
                        HanhDong = "Giảm bớt",
                        TyLeLapDayHienTai = avgOccupancy
                    });
                }
            }

            // Phân tích khung tối
            if (eveningShowtimes.Any())
            {
                var avgOccupancy = eveningShowtimes.Average(s =>
                    (s.Phong_Chieu?.Ghes.Count ?? 0) > 0
                        ? (decimal)s.Ves.Count(v => v.Dat_Ve_id != null) / s.Phong_Chieu.Ghes.Count * 100
                        : 0);

                if (avgOccupancy > 80)
                {
                    recommendations.Add(new StaffShowtimeRecommendationViewModel
                    {
                        KhungGioGoiY = "Tối",
                        LyDo = $"Tỷ lệ lấp đầy cao ({avgOccupancy:F1}%)",
                        HanhDong = "Tăng thêm",
                        TyLeLapDayHienTai = avgOccupancy
                    });
                }
            }

            return recommendations;
        }

        #endregion

        #region 2.4 Thống kê đồ ăn

        private StaffFoodStatisticsViewModel GetFoodStatistics(int rapId, int? month, int year)
        {
            // Query đơn hàng đồ ăn từ các đơn đặt vé tại rạp này
            var foodOrdersQuery = db.DonHang_DoAns
                .Where(dda => dda.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán"
                    && dda.Dat_Ve.ngay_tao.HasValue
                    && dda.Dat_Ve.Ves.Any(v => v.Suat_Chieu.Phong_Chieu.rap_id == rapId));

            if (month.HasValue)
            {
                foodOrdersQuery = foodOrdersQuery.Where(dda =>
                    dda.Dat_Ve.ngay_tao.Value.Year == year &&
                    dda.Dat_Ve.ngay_tao.Value.Month == month.Value);
            }
            else
            {
                foodOrdersQuery = foodOrdersQuery.Where(dda => dda.Dat_Ve.ngay_tao.Value.Year == year);
            }

            var foodOrders = foodOrdersQuery.ToList();

            // Món bán chạy nhất
            var topFoodItems = foodOrders
                .GroupBy(dda => dda.Do_An_id)
                .Select(g => new
                {
                    DoAnId = g.Key,
                    SoLuongBan = g.Sum(dda => dda.so_luong),
                    DoanhThu = g.Sum(dda => (decimal)(dda.Do_An.gia ?? 0) * dda.so_luong)
                })
                .OrderByDescending(x => x.SoLuongBan)
                .Take(10)
                .ToList()
                .Select(x =>
                {
                    var doAn = db.Do_Ans.FirstOrDefault(d => d.Do_An_id == x.DoAnId);
                    return new StaffFoodItemViewModel
                    {
                        DoAnId = x.DoAnId,
                        TenSanPham = doAn?.ten_san_pham ?? "N/A",
                        HinhAnh = null, // Do_An không có trường hinh_anh
                        SoLuongBan = x.SoLuongBan,
                        Gia = doAn?.gia ?? 0,
                        TongDoanhThu = x.DoanhThu,
                        LoaiDoAn = doAn?.loai ?? "N/A"
                    };
                })
                .ToList();

            // Kho đồ ăn tồn
            var inventory = db.Kho_Do_Ans
                .Where(k => k.rap_id == rapId)
                .ToList()
                .Select(k => new StaffFoodInventoryViewModel
                {
                    DoAnId = k.Do_An_id,
                    TenSanPham = k.Do_An?.ten_san_pham ?? "N/A",
                    SoLuongTon = k.so_luong_ton ?? 0,
                    NguyenDuongCanhBao = 20,
                    TrangThai = (k.so_luong_ton ?? 0) == 0 ? "Hết hàng" :
                                (k.so_luong_ton ?? 0) < 20 ? "Sắp hết" : "Đủ",
                    NgayNhapGanNhat = null // Kho_Do_An không có trường ngay_nhap_gan_nhat
                })
                .ToList();

            // Doanh thu theo món
            var totalFoodRevenue = foodOrders.Sum(dda => (decimal)(dda.Do_An.gia ?? 0) * dda.so_luong);
            var revenueByFood = foodOrders
                .GroupBy(dda => dda.Do_An_id)
                .Select(g => new StaffFoodRevenueViewModel
                {
                    DoAnId = g.Key,
                    TenSanPham = g.First().Do_An?.ten_san_pham ?? "N/A",
                    SoLuongBan = g.Sum(dda => dda.so_luong),
                    DoanhThu = g.Sum(dda => (decimal)(dda.Do_An.gia ?? 0) * dda.so_luong),
                    PhanTramDoanhThu = totalFoodRevenue > 0 
                        ? g.Sum(dda => (decimal)(dda.Do_An.gia ?? 0) * dda.so_luong) / totalFoodRevenue * 100 
                        : 0
                })
                .OrderByDescending(x => x.DoanhThu)
                .ToList();

            // Doanh thu hôm nay
            var today = DateTime.Today;
            var todayFoodOrders = db.DonHang_DoAns
                .Where(dda => dda.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán"
                    && dda.Dat_Ve.ngay_tao.HasValue
                    && dda.Dat_Ve.ngay_tao.Value.Date == today
                    && dda.Dat_Ve.Ves.Any(v => v.Suat_Chieu.Phong_Chieu.rap_id == rapId))
                .ToList();

            return new StaffFoodStatisticsViewModel
            {
                MonBanChayNhat = topFoodItems,
                KhoDoAnTon = inventory,
                DoanhThuTheoMon = revenueByFood,
                TongDoanhThuDoAnHomNay = todayFoodOrders.Sum(dda => (decimal)(dda.Do_An.gia ?? 0) * dda.so_luong),
                TongDoanhThuDoAnTheoFilter = totalFoodRevenue,
                TongMonBanHomNay = todayFoodOrders.Sum(dda => dda.so_luong),
                TyLeKhachMuaCombo = CalculateComboRate(rapId, month, year)
            };
        }

        private decimal CalculateComboRate(int rapId, int? month, int year)
        {
            var bookingsQuery = db.Dat_Ves
                .Where(d => d.trang_thai_Dat_Ve == "Đã Thanh toán"
                    && d.ngay_tao.HasValue
                    && d.Ves.Any(v => v.Suat_Chieu.Phong_Chieu.rap_id == rapId));

            if (month.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(d =>
                    d.ngay_tao.Value.Year == year &&
                    d.ngay_tao.Value.Month == month.Value);
            }
            else
            {
                bookingsQuery = bookingsQuery.Where(d => d.ngay_tao.Value.Year == year);
            }

            var totalBookings = bookingsQuery.Count();
            var bookingsWithFood = bookingsQuery.Count(d => d.DonHang_DoAns.Any());

            return totalBookings > 0 ? (decimal)bookingsWithFood / totalBookings * 100 : 0;
        }

        #endregion

        #region 2.5 Thống kê nhân viên cấp rạp

        private StaffEmployeeStatisticsViewModel GetEmployeeStatistics(int rapId, int? month, int year)
        {
            var employees = db.Nhan_Viens.Where(nv => nv.rap_id == rapId).ToList();

            var stat = new StaffEmployeeStatisticsViewModel
            {
                TongNhanVien = employees.Count,
                NhanVienDangLamViec = employees.Count(nv => nv.trang_thai == "Đang làm việc"),
                NhanVienNghiPhep = employees.Count(nv => nv.trang_thai == "Nghỉ phép"),
                HoatDongNhanVien = new List<StaffEmployeeActivityViewModel>()
            };

            foreach (var emp in employees.Where(e => e.trang_thai == "Đang làm việc"))
            {
                // Lấy các đơn đặt do nhân viên này xử lý (nếu có trường nhan_vien_id trong Dat_Ve)
                var bookingsQuery = db.Dat_Ves
                    .Where(d => d.trang_thai_Dat_Ve == "Đã Thanh toán"
                        && d.ngay_tao.HasValue
                        && d.nhan_vien_id == emp.nhanvien_id
                        && d.Ves.Any(v => v.Suat_Chieu.Phong_Chieu.rap_id == rapId));

                if (month.HasValue)
                {
                    bookingsQuery = bookingsQuery.Where(d =>
                        d.ngay_tao.Value.Year == year &&
                        d.ngay_tao.Value.Month == month.Value);
                }
                else
                {
                    bookingsQuery = bookingsQuery.Where(d => d.ngay_tao.Value.Year == year);
                }

                var bookings = bookingsQuery.ToList();

                var activity = new StaffEmployeeActivityViewModel
                {
                    NhanVienId = emp.nhanvien_id,
                    HoTen = emp.ho_ten,
                    ChucVu = emp.Role?.ten_role ?? "N/A",

                    SoVeBan = bookings.SelectMany(b => b.Ves).Count(),
                    DoanhThuBanVe = bookings.SelectMany(b => b.Ves).Sum(v => (decimal?)v.gia_ve) ?? 0,

                    SoGiaoDich = bookings.Count,
                    SoGiaoDichThanhCong = bookings.Count,
                    SoGiaoDichThatBai = 0, // Cần thêm logic tracking lỗi
                    TyLeThanhCong = bookings.Count > 0 ? 100 : 0,

                    SoKhachDuocHoTro = bookings.Select(b => b.khach_hang_id).Distinct().Count()
                };

                stat.HoatDongNhanVien.Add(activity);
            }

            // Nhân viên xuất sắc
            stat.NhanVienXuatSac = stat.HoatDongNhanVien
                .OrderByDescending(a => a.DoanhThuBanVe)
                .FirstOrDefault();

            // Nhân viên cần hỗ trợ (demo - cần logic tracking lỗi thực tế)
            stat.NhanVienCanHoTro = new List<StaffEmployeeErrorViewModel>();

            return stat;
        }

        #endregion

        #region Chart Data

        private object GetChartData(int rapId, int? month, int year)
        {
            var labels = new List<string>();
            var doanhThuVe = new List<decimal>();
            var doanhThuDoAn = new List<decimal>();
            var tongDoanhThu = new List<decimal>();

            if (month.HasValue)
            {
                // Theo ngày trong tháng
                int daysInMonth = DateTime.DaysInMonth(year, month.Value);
                for (int day = 1; day <= daysInMonth; day++)
                {
                    var date = new DateTime(year, month.Value, day);
                    labels.Add(date.ToString("dd/MM"));

                    // ✅ LẤY VÉ THEO NGÀY SUẤT CHIẾU (KHÔNG PHẢI NGÀY ĐẶT)
                    var tickets = db.Ves
                        .Where(v => v.Dat_Ve != null 
                            && v.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán"
                            && v.Suat_Chieu.ngay_chieu == date
                            && v.Suat_Chieu.Phong_Chieu.rap_id == rapId)
                        .ToList();

                    // ✅ Doanh thu vé
                    var veRevenue = tickets.Sum(v => (decimal?)v.gia_ve) ?? 0;

                    // ✅ Doanh thu đồ ăn - Lấy từ các đơn đặt có vé trong ngày suất chiếu này
                    var datVeIds = tickets.Select(v => v.Dat_Ve_id).Distinct().ToList();
                    var foodRevenue = db.DonHang_DoAns
                        .Where(dda => datVeIds.Contains(dda.Dat_Ve_id))
                        .Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0;

                    doanhThuVe.Add(veRevenue);
                    doanhThuDoAn.Add(foodRevenue);
                    tongDoanhThu.Add(veRevenue + foodRevenue);
                }
            }
            else
            {
                // Theo tháng trong năm
                for (int m = 1; m <= 12; m++)
                {
                    labels.Add($"T{m}");

                    // ✅ LẤY VÉ THEO THÁNG SUẤT CHIẾU (KHÔNG PHẢI THÁNG ĐẶT)
                    var tickets = db.Ves
                        .Where(v => v.Dat_Ve != null 
                            && v.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán"
                            && v.Suat_Chieu.ngay_chieu.Year == year
                            && v.Suat_Chieu.ngay_chieu.Month == m
                            && v.Suat_Chieu.Phong_Chieu.rap_id == rapId)
                        .ToList();

                    // ✅ Doanh thu vé
                    var veRevenue = tickets.Sum(v => (decimal?)v.gia_ve) ?? 0;

                    // ✅ Doanh thu đồ ăn - Lấy từ các đơn đặt có vé trong tháng suất chiếu này
                    var datVeIds = tickets.Select(v => v.Dat_Ve_id).Distinct().ToList();
                    var foodRevenue = db.DonHang_DoAns
                        .Where(dda => datVeIds.Contains(dda.Dat_Ve_id))
                        .Sum(dda => (decimal?)(dda.Do_An.gia ?? 0) * dda.so_luong) ?? 0;

                    doanhThuVe.Add(veRevenue);
                    doanhThuDoAn.Add(foodRevenue);
                    tongDoanhThu.Add(veRevenue + foodRevenue);
                }
            }

            return new
            {
                Labels = labels,
                DoanhThuVe = doanhThuVe,
                DoanhThuDoAn = doanhThuDoAn,
                TongDoanhThu = tongDoanhThu
            };
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
