using System;
using System.Collections.Generic;
using System.Linq;
using WebCinema.Models;
using System.Data; // Thêm dòng này để dùng ConnectionState

namespace WebCinema.Services
{
    // ViewModel hỗ trợ hiển thị
    public class StockModel
    {
        public int DoAnId { get; set; }
        public int SoLuong { get; set; }
    }
    public class SlowItemViewModel
    {
        public Do_An DoAn { get; set; }
        public int SoLuongTon { get; set; }
        public int DaBan30Ngay { get; set; }
    }
    public class FoodService
    {
        private readonly CSDLDataContext _db;

        public FoodService()
        {
            _db = new CSDLDataContext();
        }

        public FoodService(CSDLDataContext db)
        {
            _db = db;
        }

        #region 1. THỐNG KÊ DOANH SỐ (Có thể lọc theo Rạp)

        /// <summary>
        /// Thống kê sản phẩm bán chạy. 
        /// Nếu rapId = null -> Tính toàn hệ thống.
        /// </summary>
        public List<dynamic> GetTopSellingItems(int? rapId, int? month, int? year, int take = 10)
        {
            // Bắt đầu từ bảng chi tiết đơn hàng đồ ăn
            var query = _db.DonHang_DoAns.AsQueryable();

            // Chỉ lấy đơn đã thanh toán
            query = query.Where(dh => dh.Dat_Ve != null
                                   && dh.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán"
                                   && dh.Dat_Ve.ngay_tao.HasValue);

            // Lọc theo Rạp (Thông qua nhân viên bán vé)
            // Logic: DonHang -> DatVe -> NhanVien -> Rap
            if (rapId.HasValue)
            {
                query = query.Where(dh => dh.Dat_Ve.Nhan_Vien != null && dh.Dat_Ve.Nhan_Vien.rap_id == rapId.Value);
            }

            // Lọc theo thời gian
            if (year.HasValue)
            {
                query = query.Where(dh => dh.Dat_Ve.ngay_tao.Value.Year == year.Value);
            }

            if (month.HasValue)
            {
                query = query.Where(dh => dh.Dat_Ve.ngay_tao.Value.Month == month.Value);
            }

            // Group by và tính tổng
            var result = query
                .GroupBy(dh => dh.Do_An_id)
                .Select(g => new
                {
                    Do_An_id = g.Key,
                    TotalQuantity = g.Sum(dh => dh.so_luong),
                    // Lưu ý: Giá này là giá hiện tại, thực tế nên lưu giá lúc bán vào DonHang_DoAn
                    TotalRevenue = g.Sum(dh => dh.so_luong * (dh.Do_An.gia ?? 0))
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(take)
                .ToList() // Thực thi query tại đây
                .Select(x => new
                {
                    DoAn = _db.Do_Ans.FirstOrDefault(d => d.Do_An_id == x.Do_An_id),
                    TotalQuantity = x.TotalQuantity,
                    TotalRevenue = x.TotalRevenue
                })
                .Where(x => x.DoAn != null)
                .ToList<dynamic>();

            return result;
        }

        #endregion

        #region 2. QUẢN LÝ TỒN KHO (Theo Rạp)

        /// <summary>
        /// Lấy số lượng tồn của một món tại một rạp cụ thể
        /// </summary>
        public int GetStockQuantity(int rapId, int doAnId)
        {
            var khoItem = _db.Kho_Do_Ans.FirstOrDefault(k => k.rap_id == rapId && k.Do_An_id == doAnId);
            return khoItem?.so_luong_ton ?? 0;
        }

        /// <summary>
        /// Lấy danh sách các món sắp hết hàng tại một rạp (để cảnh báo)
        /// </summary>
        public List<StaffInventoryViewModel> GetLowStockItems(int rapId, int threshold = 20)
        {
            // Join Do_An với Kho_Do_An để lấy thông tin chi tiết
            var query = from d in _db.Do_Ans
                        join k in _db.Kho_Do_Ans
                             on new { d.Do_An_id, RapId = rapId } equals new { k.Do_An_id, RapId = k.rap_id } into khoGroup
                        from k in khoGroup.DefaultIfEmpty()
                        where d.trang_thai == "Đang kinh doanh" // Chỉ cảnh báo món đang kinh doanh
                        select new { DoAn = d, SoLuong = (k != null ? k.so_luong_ton : 0) };

            // Lọc các món có số lượng <= ngưỡng (threshold)
            var result = query.Where(x => x.SoLuong <= threshold)
                              .OrderBy(x => x.SoLuong)
                              .ToList()
                              .Select(x => new StaffInventoryViewModel
                              {
                                  Do_An_id = x.DoAn.Do_An_id,
                                  TenSanPham = x.DoAn.ten_san_pham,
                                  Loai = x.DoAn.loai,
                                  SoLuongTon = x.SoLuong.Value // int? -> int
                              })
                              .ToList();
            return result;
        }

        /// <summary>
        /// Tạo các thông báo cảnh báo để hiển thị lên Dashboard
        /// </summary>
        public List<string> GetStockWarnings(int rapId)
        {
            var warnings = new List<string>();

            // Lấy danh sách dưới mức an toàn (ví dụ < 10)
            var lowStockItems = GetLowStockItems(rapId, 10);

            var outOfStockCount = lowStockItems.Count(x => x.SoLuongTon <= 0);
            var lowStockCount = lowStockItems.Count(x => x.SoLuongTon > 0);

            if (outOfStockCount > 0)
            {
                warnings.Add($"⚠️ CẢNH BÁO: Có {outOfStockCount} món đã HẾT HÀNG tại rạp!");
            }

            if (lowStockCount > 0)
            {
                warnings.Add($"⚠️ CHÚ Ý: Có {lowStockCount} món sắp hết hàng (dưới 10).");
            }

            if (warnings.Count == 0)
            {
                warnings.Add("✅ Kho hàng ổn định.");
            }

            return warnings;
        }

        #endregion

        #region 3. NGHIỆP VỤ NHẬP KHO (Transaction)

        /// <summary>
        /// Xử lý logic nhập kho: Tạo phiếu nhập -> Thêm chi tiết -> Cập nhật kho
        /// </summary>
        // Trong FoodService.cs

        public bool ProcessImport(int nhanVienId, int rapId, List<Chi_Tiet_Phieu_Nhap> chiTietList, string ghiChu)
        {
            using (var transaction = _db.Connection.BeginTransaction())
            {
                _db.Transaction = transaction;
                try
                {
                    // 1. Tạo phiếu nhập với trạng thái
                    var phieuNhap = new Phieu_Nhap
                    {
                        nhan_vien_id = nhanVienId,
                        rap_id = rapId,
                        ngay_nhap = DateTime.Now,
                        ghi_chu = ghiChu,

                        // --- CẬP NHẬT Ở ĐÂY ---
                        // Nếu muốn nhập luôn: set "Đã nhập"
                        // Nếu muốn chờ quản lý duyệt: set "Chờ duyệt"
                        trang_thai = "Đã nhập"
                    };

                    _db.Phieu_Nhaps.InsertOnSubmit(phieuNhap);
                    _db.SubmitChanges();

                    // 2. Xử lý chi tiết
                    foreach (var item in chiTietList)
                    {
                        item.phieu_nhap_id = phieuNhap.phieu_nhap_id;
                        _db.Chi_Tiet_Phieu_Nhaps.InsertOnSubmit(item);

                        // --- QUAN TRỌNG ---
                        // Nếu trạng thái là "Đã nhập" thì mới cộng kho
                        if (phieuNhap.trang_thai == "Đã nhập")
                        {
                            var khoItem = _db.Kho_Do_Ans.FirstOrDefault(k => k.rap_id == rapId && k.Do_An_id == item.do_an_id);
                            if (khoItem != null)
                            {
                                khoItem.so_luong_ton += item.so_luong_nhap;
                            }
                            else
                            {
                                var newKhoItem = new Kho_Do_An
                                {
                                    rap_id = rapId,
                                    Do_An_id = item.do_an_id,
                                    so_luong_ton = item.so_luong_nhap
                                };
                                _db.Kho_Do_Ans.InsertOnSubmit(newKhoItem);
                            }
                        }
                    }

                    _db.SubmitChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        #endregion
        public bool CreateImportRequest(int nhanVienId, int rapId, List<Chi_Tiet_Phieu_Nhap> chiTietList, string ghiChu)
        {
            try
            {
                // 1. Tạo phiếu nhập
                var phieuNhap = new Phieu_Nhap
                {
                    nhan_vien_id = nhanVienId,
                    rap_id = rapId,
                    ngay_nhap = DateTime.Now,
                    ghi_chu = ghiChu,
                    trang_thai = "Chờ duyệt" // Mặc định là chờ duyệt
                };
                _db.Phieu_Nhaps.InsertOnSubmit(phieuNhap);
                _db.SubmitChanges(); // Để lấy ID

                // 2. Thêm chi tiết (Chưa cộng kho)
                foreach (var item in chiTietList)
                {
                    item.phieu_nhap_id = phieuNhap.phieu_nhap_id;
                    _db.Chi_Tiet_Phieu_Nhaps.InsertOnSubmit(item);
                }

                _db.SubmitChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // BƯỚC 3: Nhân viên xác nhận hàng đã về (Cộng kho)
        // Hàm này chỉ chạy được khi phiếu đang ở trạng thái "Đã duyệt" (Do quản lý set)
        public string ConfirmReceipt(int phieuNhapId, int nhanVienId)
        {
            if (_db.Connection.State == ConnectionState.Closed)
            {
                _db.Connection.Open();
            }
            using (var transaction = _db.Connection.BeginTransaction())
            {
                _db.Transaction = transaction;
                try
                {
                    var phieu = _db.Phieu_Nhaps.FirstOrDefault(p => p.phieu_nhap_id == phieuNhapId);

                    if (phieu == null) return "Phiếu không tồn tại";

                    // Chỉ cho phép nhập khi trạng thái là "Đã duyệt"
                    // (Lưu ý: Để test được ngay, bạn có thể tạm cho phép cả "Chờ duyệt" nếu chưa làm trang Admin)
                    if (phieu.trang_thai != "Đã duyệt" && phieu.trang_thai != "Chờ duyệt")
                    {
                        return "Phiếu này chưa được duyệt hoặc đã nhập rồi.";
                    }

                    // 1. Cập nhật trạng thái và người xác nhận cuối cùng (nếu cần)
                    phieu.trang_thai = "Đã nhập";
                    phieu.ngay_nhap = DateTime.Now; // Cập nhật lại ngày thực tế nhập

                    // 2. Cộng kho
                    var chiTietList = _db.Chi_Tiet_Phieu_Nhaps.Where(ct => ct.phieu_nhap_id == phieuNhapId).ToList();
                    foreach (var item in chiTietList)
                    {
                        var khoItem = _db.Kho_Do_Ans.FirstOrDefault(k => k.rap_id == phieu.rap_id && k.Do_An_id == item.do_an_id);
                        if (khoItem != null)
                        {
                            khoItem.so_luong_ton += item.so_luong_nhap;
                        }
                        else
                        {
                            var newKhoItem = new Kho_Do_An
                            {
                                rap_id = phieu.rap_id,
                                Do_An_id = item.do_an_id,
                                so_luong_ton = item.so_luong_nhap
                            };
                            _db.Kho_Do_Ans.InsertOnSubmit(newKhoItem);
                        }
                    }

                    _db.SubmitChanges();
                    transaction.Commit();
                    return "Success";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return "Lỗi hệ thống: " + ex.Message;
                }
            }
        }
        // --- Tìm món bán chậm (Tồn > 20 nhưng bán < 10 trong 30 ngày) ---
        public List<SlowItemViewModel> GetSlowSellingItems(int rapId)
        {
            var oneMonthAgo = DateTime.Now.AddDays(-30);
            var inventory = _db.Kho_Do_Ans.Where(k => k.rap_id == rapId && k.so_luong_ton > 20).ToList();
            var result = new List<SlowItemViewModel>();

            foreach (var item in inventory)
            {
                int soldCount = _db.DonHang_DoAns
                    .Where(dh => dh.Do_An_id == item.Do_An_id
                                 && dh.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán"
                                 && dh.Dat_Ve.ngay_tao >= oneMonthAgo
                                 && dh.Dat_Ve.Nhan_Vien.rap_id == rapId)
                    .Sum(dh => (int?)dh.so_luong) ?? 0;

                if (soldCount < 10) // Ngưỡng bán chậm
                {
                    result.Add(new SlowItemViewModel { DoAn = item.Do_An, SoLuongTon = item.so_luong_ton ?? 0, DaBan30Ngay = soldCount });
                }
            }
            return result.OrderByDescending(x => x.SoLuongTon).ToList();
        }

        // --- Tạo phiếu đề xuất (Transaction) ---
        public bool CreatePromotionProposal(int nhanVienId, int rapId, string reason, List<Chi_Tiet_De_Xuat_Khuyen_Mai> details)
        {
            if (_db.Connection.State == ConnectionState.Closed)
            {
                _db.Connection.Open();
            }
            using (var transaction = _db.Connection.BeginTransaction())
            {
                _db.Transaction = transaction;
                try
                {
                    // 1. Lưu Master
                    var phieu = new Phieu_De_Xuat_Khuyen_Mai
                    {
                        nhan_vien_id = nhanVienId,
                        rap_id = rapId,
                        ly_do = reason,
                        trang_thai = "Chờ duyệt",
                        ngay_tao = DateTime.Now
                    };
                    _db.Phieu_De_Xuat_Khuyen_Mais.InsertOnSubmit(phieu);
                    _db.SubmitChanges();

                    // 2. Lưu Details
                    foreach (var item in details)
                    {
                        var kho = _db.Kho_Do_Ans.FirstOrDefault(k => k.rap_id == rapId && k.Do_An_id == item.do_an_id);
                        item.de_xuat_id = phieu.de_xuat_id;
                        item.so_luong_ton = kho != null ? (kho.so_luong_ton ?? 0) : 0;
                        _db.Chi_Tiet_De_Xuat_Khuyen_Mais.InsertOnSubmit(item);
                    }
                    _db.SubmitChanges();
                    transaction.Commit();
                    return true;
                }
                catch { transaction.Rollback(); return false; }
            }
        }
        public bool UpdateStock(int rapId, List<StockModel> items, bool isAdding)
        {
            // Mở kết nối nếu đang đóng
            if (_db.Connection.State == ConnectionState.Closed)
            {
                _db.Connection.Open();
            }

            using (var transaction = _db.Connection.BeginTransaction())
            {
                _db.Transaction = transaction;
                try
                {
                    foreach (var item in items)
                    {
                        var khoItem = _db.Kho_Do_Ans.FirstOrDefault(k => k.rap_id == rapId && k.Do_An_id == item.DoAnId);

                        if (isAdding)
                        {
                            // --- TRƯỜNG HỢP NHẬP KHO (+) ---
                            if (khoItem != null)
                            {
                                khoItem.so_luong_ton += item.SoLuong;
                            }
                            else
                            {
                                // Chưa có thì tạo mới
                                var newItem = new Kho_Do_An
                                {
                                    rap_id = rapId,
                                    Do_An_id = item.DoAnId,
                                    so_luong_ton = item.SoLuong
                                };
                                _db.Kho_Do_Ans.InsertOnSubmit(newItem);
                            }
                        }
                        else
                        {
                            // --- TRƯỜNG HỢP BÁN HÀNG (-) ---
                            if (khoItem != null)
                            {
                                khoItem.so_luong_ton -= item.SoLuong;

                                // Đảm bảo không âm
                                if (khoItem.so_luong_ton < 0) khoItem.so_luong_ton = 0;
                            }
                            // Nếu khoItem == null (lỗi dữ liệu) thì bỏ qua hoặc log lại tùy ý
                        }
                    }

                    _db.SubmitChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // Ghi log lỗi tại đây nếu cần
                    return false;
                }
            }
        }
    }
}