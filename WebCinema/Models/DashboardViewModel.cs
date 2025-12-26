using System;
using System.Collections.Generic;

namespace WebCinema.Models
{
    #region Dashboard ViewModels

    /// <summary>
    /// ViewModel t?ng quát cho Dashboard Admin
    /// </summary>
    public class AdminDashboardViewModel
    {
        // 1.1. Dashboard t?ng h?p
        public DashboardSummaryViewModel Summary { get; set; }

        // 1.2. Th?ng kê theo r?p
        public List<CinemaStatisticViewModel> CinemaStatistics { get; set; }

        // 1.3. Th?ng kê theo phim
        public MovieStatisticsViewModel MovieStatistics { get; set; }

        // 1.4. Th?ng kê ?? ?n & combo
        public FoodStatisticsViewModel FoodStatistics { get; set; }

        // 1.5. Th?ng kê khách hàng
        public CustomerStatisticsViewModel CustomerStatistics { get; set; }

        // 1.6. Th?ng kê nhân viên
        public StaffStatisticsViewModel StaffStatistics { get; set; }

        // Filters
        public DateTime? FilterDate { get; set; }
        public int? FilterMonth { get; set; }
        public int? FilterYear { get; set; }
    }

    #endregion

    #region 1.1. Dashboard T?ng H?p

    public class DashboardSummaryViewModel
    {
        // Hôm nay
        public decimal TongDoanhThuHomNay { get; set; }
        public int TongVeBanHomNay { get; set; }
        public int TongVeHuyHomNay { get; set; }
        public decimal TongDoanhThuComboHomNay { get; set; }
        public int SoKhachMoiDangKyHomNay { get; set; }
        public int SoSuatChieuHomNay { get; set; }

        // T?ng h? th?ng (toàn th?i gian ho?c theo filter)
        public decimal TongDoanhThuHeThong { get; set; }
        public int TongVeBanHeThong { get; set; }
        public int TongKhachHang { get; set; }
        public int TongPhim { get; set; }
        public int TongRap { get; set; }
    }

    #endregion

    #region 1.2. Th?ng Kê Theo R?p

    public class CinemaStatisticViewModel
    {
        public int RapId { get; set; }
        public string TenRap { get; set; }
        public string DiaChi { get; set; }

        // Doanh thu
        public decimal DoanhThuVe { get; set; }
        public decimal DoanhThuDoAn { get; set; }
        public decimal TongDoanhThu { get; set; }

        // Vé bán ra
        public int SoVeBan { get; set; }
        public int SoVeHuy { get; set; }

        // T? l? l?p ??y
        public int TongSoGhe { get; set; }
        public int SoGheDaBan { get; set; }
        public decimal TyLeLapDay { get; set; } // %

        // Su?t chi?u
        public int SoSuatChieu { get; set; }

        // X?p h?ng
        public int XepHangDoanhThu { get; set; }
        public int XepHangSoVe { get; set; }
    }

    #endregion

    #region 1.3. Th?ng Kê Theo Phim

    public class MovieStatisticsViewModel
    {
        // Top phim doanh thu cao
        public List<MovieRevenueViewModel> TopPhimDoanhThuCao { get; set; }

        // Phim bán ch?y nh?t
        public List<MovieRevenueViewModel> TopPhimBanChay { get; set; }

        // Phim b? ?
        public List<MovieRevenueViewModel> PhimBiE { get; set; }

        // Su?t chi?u ho?t ??ng t?t
        public List<ShowtimePerformanceViewModel> SuatChieuTotNhat { get; set; }

        // So sánh phim gi?a các r?p
        public List<MovieCinemaComparisonViewModel> SoSanhPhimTheoRap { get; set; }
    }

    public class MovieRevenueViewModel
    {
        public int PhimId { get; set; }
        public string TenPhim { get; set; }
        public string AnhBia { get; set; }
        public int SoVeBan { get; set; }
        public decimal DoanhThuVe { get; set; }
        public decimal DoanhThuDoAn { get; set; }
        public decimal TongDoanhThu { get; set; }
        public int SoSuatChieu { get; set; }
        public decimal TyLeLapDay { get; set; }
    }

    public class ShowtimePerformanceViewModel
    {
        public int SuatChieuId { get; set; }
        public string TenPhim { get; set; }
        public string TenRap { get; set; }
        public string TenPhong { get; set; }
        public DateTime NgayChieu { get; set; }
        public TimeSpan GioChieu { get; set; }
        public int SoVeBan { get; set; }
        public int TongSoGhe { get; set; }
        public decimal TyLeLapDay { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class MovieCinemaComparisonViewModel
    {
        public int PhimId { get; set; }
        public string TenPhim { get; set; }
        public List<CinemaRevenueDetail> ChiTietTheoRap { get; set; }
    }

    public class CinemaRevenueDetail
    {
        public int RapId { get; set; }
        public string TenRap { get; set; }
        public int SoVeBan { get; set; }
        public decimal DoanhThu { get; set; }
        public int SoSuatChieu { get; set; }
    }

    #endregion

    #region 1.4. Th?ng Kê ?? ?n & Combo

    public class FoodStatisticsViewModel
    {
        // Món bán ch?y nh?t toàn h? th?ng
        public List<FoodItemStatViewModel> MonBanChayNhat { get; set; }

        // Doanh thu ?? ?n theo r?p
        public List<FoodRevenueByCinemaViewModel> DoanhThuDoAnTheoRap { get; set; }

        // T? l? khách mua combo
        public decimal TyLeKhachMuaCombo { get; set; }
        public int TongKhachMuaVe { get; set; }
        public int SoKhachMuaCombo { get; set; }

        // T?ng ti?n gi?m qua mã khuy?n mãi
        public decimal TongTienGiamKhuyenMai { get; set; }
        public int SoLuotSuDungKhuyenMai { get; set; }
    }

    public class FoodItemStatViewModel
    {
        public int DoAnId { get; set; }
        public string TenSanPham { get; set; }
        public string HinhAnh { get; set; }
        public int SoLuongBan { get; set; }
        public decimal Gia { get; set; }
        public decimal TongDoanhThu { get; set; }
        public string LoaiDoAn { get; set; }
    }

    public class FoodRevenueByCinemaViewModel
    {
        public int RapId { get; set; }
        public string TenRap { get; set; }
        public decimal TongDoanhThuDoAn { get; set; }
        public int SoLuongMonBan { get; set; }
        public List<FoodItemStatViewModel> TopMonBanChay { get; set; }
    }

    #endregion

    #region 1.5. Th?ng Kê Khách Hàng

    public class CustomerStatisticsViewModel
    {
        // T?ng khách
        public int TongKhachDangKy { get; set; }
        public int KhachMoiTrongThang { get; set; }
        public int KhachVIP { get; set; } // Khách có ?i?m tích l?y cao

        // Top khách mua nhi?u
        public List<TopCustomerViewModel> TopKhachMuaNhieu { get; set; }

        // Phân tích hành vi
        public decimal SoTienTrungBinhMoiDon { get; set; }
        public decimal SoVeTrungBinhMoiKhach { get; set; }
    }

    public class TopCustomerViewModel
    {
        public int KhachHangId { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string SoDienThoai { get; set; }
        public int SoDonDat { get; set; }
        public int SoVeMua { get; set; }
        public decimal TongChiTieu { get; set; }
        public int DiemTichLuy { get; set; }
    }

    #endregion

    #region 1.6. Th?ng Kê Nhân Viên

    public class StaffStatisticsViewModel
    {
        // T?ng quan
        public int TongNhanVien { get; set; }
        public int NhanVienDangLamViec { get; set; }

        // Chi ti?t ho?t ??ng
        public List<StaffActivityViewModel> HoatDongNhanVien { get; set; }
    }

    public class StaffActivityViewModel
    {
        public int NhanVienId { get; set; }
        public string HoTen { get; set; }
        public string ChucVu { get; set; }
        public string TenRap { get; set; }

        // Ho?t ??ng bán vé
        public int SoVeBan { get; set; }
        public decimal DoanhThuBanVe { get; set; }

        // Giao d?ch
        public int SoGiaoDichThanhCong { get; set; }
        public int SoGiaoDichThatBai { get; set; }

        // H? tr? khách hàng
        public int SoKhachDuocHoTro { get; set; }
    }

    #endregion

    #region Helper ViewModels

    public class RevenueChartDataViewModel
    {
        public List<string> Labels { get; set; }
        public List<decimal> DoanhThuVe { get; set; }
        public List<decimal> DoanhThuDoAn { get; set; }
        public List<decimal> TongDoanhThu { get; set; }
    }

    #endregion

    #region 2. STAFF DASHBOARD - Th?ng Kê Theo R?p

    /// <summary>
    /// ViewModel cho Staff Dashboard - Ch? xem d? li?u r?p c?a mình
    /// </summary>
    public class StaffDashboardViewModel
    {
        // Thông tin r?p hi?n t?i
        public int RapId { get; set; }
        public string TenRap { get; set; }
        public string DiaChiRap { get; set; }

        // Filter
        public int? FilterMonth { get; set; }
        public int FilterYear { get; set; }

        // 2.1 Dashboard cho Staff
        public StaffSummaryViewModel Summary { get; set; }

        // 2.2 Th?ng kê phòng chi?u
        public List<StaffRoomStatisticsViewModel> RoomStatistics { get; set; }

        // 2.3 Th?ng kê theo phim (t?i r?p)
        public StaffMovieStatisticsViewModel MovieStatistics { get; set; }

        // 2.4 Combo - ?? ?n
        public StaffFoodStatisticsViewModel FoodStatistics { get; set; }

        // 2.5 Th?ng kê nhân viên (c?p r?p)
        public StaffEmployeeStatisticsViewModel EmployeeStatistics { get; set; }

        // Bi?u ?? doanh thu
        public object ChartData { get; set; }
    }

    #region 2.1 Staff Summary

    public class StaffSummaryViewModel
    {
        // Hôm nay
        public decimal TongDoanhThuHomNay { get; set; }
        public int TongVeBanHomNay { get; set; }
        public int TongVeHuyHomNay { get; set; }
        public int SoSuatChieuHomNay { get; set; }
        public decimal TongDoanhThuComboHomNay { get; set; }
        public int SoKhachMoiDangKyHomNay { get; set; } // N?u staff ??ng ký khách t?i qu?y

        // Theo filter (tháng/n?m)
        public decimal TongDoanhThuTheoFilter { get; set; }
        public int TongVeBanTheoFilter { get; set; }
        public int TongVeHuyTheoFilter { get; set; }

        // T?ng quan r?p
        public int TongPhongChieu { get; set; }
        public int TongGhe { get; set; }
        public int TongNhanVien { get; set; }
    }

    #endregion

    #region 2.2 Th?ng kê phòng chi?u

    public class StaffRoomStatisticsViewModel
    {
        public int PhongId { get; set; }
        public string TenPhong { get; set; }
        public int SoGhe { get; set; }
        
        // Su?t chi?u trong ngày
        public int SoSuatChieuHomNay { get; set; }
        public List<StaffShowtimeInRoomViewModel> SuatChieuHomNay { get; set; }

        // T? l? gh? ??y
        public decimal TyLeLapDayTrungBinh { get; set; }
        public int TongVeBan { get; set; }
        public int TongGheTrong { get; set; }

        // Doanh thu
        public decimal DoanhThu { get; set; }

        // Tr?ng thái thi?t b?
        public string TrangThaiThietBi { get; set; } // "Bình th??ng", "L?i nh?", "C?n b?o trì"
        public string GhiChuLoi { get; set; }
    }

    public class StaffShowtimeInRoomViewModel
    {
        public int SuatChieuId { get; set; }
        public string TenPhim { get; set; }
        public TimeSpan GioBatDau { get; set; }
        public int SoVeBan { get; set; }
        public int TongGhe { get; set; }
        public decimal TyLeLapDay { get; set; }
        public string TrangThai { get; set; } // "S?p chi?u", "?ang chi?u", "?ã chi?u"
    }

    #endregion

    #region 2.3 Th?ng kê phim t?i r?p

    public class StaffMovieStatisticsViewModel
    {
        // Phim bán ch?y
        public List<StaffMoviePerformanceViewModel> PhimBanChayNhat { get; set; }

        // Phim l? vé
        public List<StaffMoviePerformanceViewModel> PhimLoVe { get; set; }

        // Su?t chi?u ?ông khách nh?t
        public List<StaffTopShowtimeViewModel> SuatChieuDongKhachNhat { get; set; }

        // D? ?oán su?t t?i ?u
        public List<StaffShowtimeRecommendationViewModel> DuDoanSuatToiUu { get; set; }
    }

    public class StaffMoviePerformanceViewModel
    {
        public int PhimId { get; set; }
        public string TenPhim { get; set; }
        public string HinhAnh { get; set; }
        public int SoVeBan { get; set; }
        public decimal DoanhThu { get; set; }
        public int SoSuatChieu { get; set; }
        public decimal TyLeLapDayTrungBinh { get; set; }
        public decimal DoanhThuTrungBinhMoiSuat { get; set; }
    }

    public class StaffTopShowtimeViewModel
    {
        public int SuatChieuId { get; set; }
        public string TenPhim { get; set; }
        public string TenPhong { get; set; }
        public DateTime NgayChieu { get; set; }
        public TimeSpan GioChieu { get; set; }
        public int SoVeBan { get; set; }
        public int TongGhe { get; set; }
        public decimal TyLeLapDay { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class StaffShowtimeRecommendationViewModel
    {
        public string TenPhim { get; set; }
        public string TenPhong { get; set; }
        public string KhungGioGoiY { get; set; } // "Sáng", "Chi?u", "T?i", "?êm"
        public string LyDo { get; set; }
        public string HanhDong { get; set; } // "T?ng thêm", "Gi?m b?t", "Gi? nguyên"
        public decimal TyLeLapDayHienTai { get; set; }
    }

    #endregion

    #region 2.4 Staff Food Statistics

    public class StaffFoodStatisticsViewModel
    {
        // Món bán ch?y nh?t t?i r?p
        public List<StaffFoodItemViewModel> MonBanChayNhat { get; set; }

        // Kho ?? ?n t?n
        public List<StaffFoodInventoryViewModel> KhoDoAnTon { get; set; }

        // Doanh thu trong ngày theo món
        public List<StaffFoodRevenueViewModel> DoanhThuTheoMon { get; set; }

        // T?ng quan
        public decimal TongDoanhThuDoAnHomNay { get; set; }
        public decimal TongDoanhThuDoAnTheoFilter { get; set; }
        public int TongMonBanHomNay { get; set; }
        public decimal TyLeKhachMuaCombo { get; set; }
    }

    public class StaffFoodItemViewModel
    {
        public int DoAnId { get; set; }
        public string TenSanPham { get; set; }
        public string HinhAnh { get; set; }
        public int SoLuongBan { get; set; }
        public decimal Gia { get; set; }
        public decimal TongDoanhThu { get; set; }
        public string LoaiDoAn { get; set; }
    }

    public class StaffFoodInventoryViewModel
    {
        public int DoAnId { get; set; }
        public string TenSanPham { get; set; }
        public int SoLuongTon { get; set; }
        public int NguyenDuongCanhBao { get; set; } // Ng??ng c?nh báo h?t hàng
        public string TrangThai { get; set; } // "??", "S?p h?t", "H?t hàng"
        public DateTime? NgayNhapGanNhat { get; set; }
    }

    public class StaffFoodRevenueViewModel
    {
        public int DoAnId { get; set; }
        public string TenSanPham { get; set; }
        public int SoLuongBan { get; set; }
        public decimal DoanhThu { get; set; }
        public decimal PhanTramDoanhThu { get; set; } // % trong t?ng doanh thu ?? ?n
    }

    #endregion

    #region 2.5 Th?ng kê nhân viên c?p r?p

    public class StaffEmployeeStatisticsViewModel
    {
        // T?ng quan
        public int TongNhanVien { get; set; }
        public int NhanVienDangLamViec { get; set; }
        public int NhanVienNghiPhep { get; set; }

        // Ho?t ??ng t?ng nhân viên
        public List<StaffEmployeeActivityViewModel> HoatDongNhanVien { get; set; }

        // Nhân viên xu?t s?c
        public StaffEmployeeActivityViewModel NhanVienXuatSac { get; set; }

        // Nhân viên c?n h? tr? (gây l?i nhi?u)
        public List<StaffEmployeeErrorViewModel> NhanVienCanHoTro { get; set; }
    }

    public class StaffEmployeeActivityViewModel
    {
        public int NhanVienId { get; set; }
        public string HoTen { get; set; }
        public string ChucVu { get; set; }
        public string HinhAnh { get; set; }

        // Vé bán
        public int SoVeBan { get; set; }
        public decimal DoanhThuBanVe { get; set; }

        // Giao d?ch
        public int SoGiaoDich { get; set; }
        public int SoGiaoDichThanhCong { get; set; }
        public int SoGiaoDichThatBai { get; set; }
        public decimal TyLeThanhCong { get; set; }

        // H? tr? khách hàng
        public int SoKhachDuocHoTro { get; set; }
        public decimal DiemDanhGiaTrungBinh { get; set; }

        // Ca làm vi?c
        public int SoCaLamTrongThang { get; set; }
        public int SoGioLamViec { get; set; }
    }

    public class StaffEmployeeErrorViewModel
    {
        public int NhanVienId { get; set; }
        public string HoTen { get; set; }
        public int SoLoiGayRa { get; set; }
        public string LoaiLoiPhoPhien { get; set; } // Lo?i l?i hay g?p nh?t
        public DateTime NgayLoiGanNhat { get; set; }
        public string MoTaLoi { get; set; }
    }

    #endregion

    #endregion
}
