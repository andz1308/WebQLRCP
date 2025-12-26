using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebCinema.Infrastructure;
using WebCinema.Models;
// Đảm bảo namespace models trỏ đúng tới nơi chứa file .dbml hoặc Entity Framework context của bạn

namespace WebCinema.Areas.Admin.Controllers
{
    // Giả sử bạn đã có cơ chế xác thực quyền Staff
    [RoleAuthorize(Roles = "Staff")]
    public class StaffInventoryManagementController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // Helper: Lấy ID rạp của nhân viên đang đăng nhập
        // Bạn cần sửa logic này tùy theo cách bạn lưu session đăng nhập
        private int GetCurrentStaffCinemaId()
        {
            // VÍ DỤ: Lấy email từ User.Identity và truy vấn ra nhân viên
            var username = User.Identity.Name;
            var staff = db.Nhan_Viens.FirstOrDefault(nv => nv.email == username || nv.ho_ten == username); // Sửa logic tìm kiếm staff phù hợp với hệ thống login của bạn

            if (staff != null && staff.rap_id.HasValue)
            {
                return staff.rap_id.Value;
            }

            // Nếu không tìm thấy hoặc nhân viên chưa gán rạp, trả về 0 hoặc throw exception
            return 0;
        }

        // GET: Admin/StaffInventoryManagement
        public ActionResult Index(string searchTerm, string category)
        {
            int currentRapId = GetCurrentStaffCinemaId();

            if (currentRapId == 0)
            {
                // Xử lý trường hợp nhân viên không thuộc rạp nào
                ViewBag.Error = "Tài khoản của bạn chưa được gán vào Rạp chiếu phim nào.";
                return View(new List<StaffInventoryViewModel>());
            }

            // 1. Lấy danh sách món ăn
            var query = db.Do_Ans.AsQueryable();

            // 2. Filter theo từ khóa
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.ten_san_pham.Contains(searchTerm) || d.mo_ta.Contains(searchTerm));
            }

            // 3. Filter theo loại
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(d => d.loai == category);
            }

            // 4. JOIN với bảng Kho_Do_An để lấy số lượng tồn CỦA RẠP HIỆN TẠI
            // Sử dụng GroupJoin hoặc Left Join để món nào chưa có trong kho vẫn hiện ra (với tồn = 0)
            var inventoryData = from d in query
                                join k in db.Kho_Do_Ans
                                     on new { d.Do_An_id, RapId = currentRapId } equals new { k.Do_An_id, RapId = k.rap_id } into khoGroup
                                from k in khoGroup.DefaultIfEmpty() // Left Join
                                select new StaffInventoryViewModel
                                {
                                    Do_An_id = d.Do_An_id,
                                    TenSanPham = d.ten_san_pham,
                                    MoTa = d.mo_ta,
                                    Loai = d.loai,
                                    Gia = d.gia,
                                    TrangThai = d.trang_thai,
                                    // Nếu không tìm thấy trong kho thì tồn = 0
                                    SoLuongTon = k != null ? (k.so_luong_ton ?? 0) : 0
                                };

            // Sắp xếp
            var result = inventoryData.OrderBy(d => d.Loai).ThenBy(d => d.TenSanPham).ToList();

            // Dropdown Categories
            ViewBag.Categories = db.Do_Ans
                .Where(d => d.loai != null)
                .Select(d => d.loai)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedCategory = category;

            return View(result);
        }

        // GET: Admin/StaffInventoryManagement/Details/5
        public ActionResult Details(int id)
        {
            int currentRapId = GetCurrentStaffCinemaId();

            // Tìm thông tin món ăn
            var item = db.Do_Ans.FirstOrDefault(d => d.Do_An_id == id);
            if (item == null)
            {
                return HttpNotFound();
            }

            // Lấy tồn kho cụ thể tại rạp này
            var stockItem = db.Kho_Do_Ans.FirstOrDefault(k => k.Do_An_id == id && k.rap_id == currentRapId);
            int currentStock = stockItem != null ? (stockItem.so_luong_ton ?? 0) : 0;

            // Tính toán doanh số bán hàng (Chỉ tính các đơn hàng ĐÃ THANH TOÁN)
            // Cần JOIN để đảm bảo chỉ tính đơn hàng thuộc các rạp (logic phức tạp hơn nếu Dat_Ve không có rap_id trực tiếp)
            // Ở đây tôi giả định tính doanh thu toàn hệ thống cho món này, 
            // Nếu muốn tính riêng rạp, cần join: DonHang_DoAn -> Dat_Ve -> Nhan_Vien -> Rap (như schema bạn đưa)

            var sales = db.DonHang_DoAns
                .Where(dh => dh.Do_An_id == id
                             && dh.Dat_Ve != null
                             && dh.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán"); // Chuỗi trạng thái phải khớp DB

            // Nếu muốn lọc sales chỉ của rạp hiện tại (nếu Dat_Ve có liên kết NhanVien -> Rap)
            // sales = sales.Where(dh => dh.Dat_Ve.Nhan_Vien.rap_id == currentRapId);

            var totalSold = sales.Sum(dh => (int?)dh.so_luong) ?? 0;

            // Tính doanh thu (Lưu ý: Schema DonHang_DoAn không lưu giá lúc bán, nên phải dùng giá hiện tại hoặc join lại Do_An)
            // Trong thực tế nên lưu giá vào DonHang_DoAn để chính xác lịch sử giá
            var totalRevenue = sales.Sum(dh => (decimal?)(dh.so_luong * (dh.Do_An.gia ?? 0m))) ?? 0m;

            // Lấy 10 đơn hàng gần nhất có chứa món này
            var recentOrders = sales
                .OrderByDescending(dh => dh.Dat_Ve.ngay_tao)
                .Take(10)
                .Select(dh => new
                {
                    MaDon = dh.Dat_Ve_id,
                    Ngay = dh.Dat_Ve.ngay_tao,
                    SoLuong = dh.so_luong,
                    NguoiMua = dh.Dat_Ve.Khach_Hang.ho_ten
                })
                .ToList();

            // Đóng gói dữ liệu sang View
            ViewBag.StockQuantity = currentStock; // Quan trọng: Số lượng tồn kho
            ViewBag.TotalSold = totalSold;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.RecentOrders = recentOrders;

            // Trả về model gốc Do_An để hiển thị thông tin cơ bản
            return View(item);
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
    // ViewModel để hiển thị thông tin món ăn kèm số lượng tồn kho của rạp hiện tại

}