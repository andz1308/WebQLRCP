using System;
using System.Linq;
using System.Web.Mvc;
using System.Web;
using System.IO;
using System.Collections.Generic; // Thêm namespace này
using WebCinema.Models;
using WebCinema.Infrastructure;
using WebCinema.Services;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Admin")] // Đổi role cho phù hợp
    public class InventoryManagementController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();
        private FoodService _foodService = new FoodService();

        // Helper: Lấy ID Rạp của Quản lý đang đăng nhập
        private int? GetCurrentRapId()
        {
            var username = User.Identity.Name;
            var staff = db.Nhan_Viens.FirstOrDefault(nv => nv.email == username || nv.ho_ten == username);
            return staff?.rap_id;
        }

        // GET: Admin/InventoryManagement
        // Hiển thị danh sách món ăn và Tồn kho tại rạp của Quản lý
        public ActionResult Index(string searchTerm, string category)
        {
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";

            int? currentRapId = GetCurrentRapId();
            if (currentRapId == null)
            {
                // Nếu là Admin tổng (không thuộc rạp nào), có thể xử lý khác. 
                // Ở đây giả định quản lý phải thuộc 1 rạp.
                ViewBag.Error = "Tài khoản không thuộc rạp chiếu phim nào.";
            }

            // 1. Lấy danh sách món ăn
            var query = db.Do_Ans.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.ten_san_pham.Contains(searchTerm) || d.mo_ta.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(d => d.loai == category);
            }

            // 2. JOIN với Kho_Do_An để lấy số lượng tồn CỦA RẠP HIỆN TẠI
            // Dùng GroupJoin (Left Join) để vẫn hiện món ăn dù trong kho chưa có record
            var result = from d in query
                         join k in db.Kho_Do_Ans
                              on new { d.Do_An_id, RapId = currentRapId.GetValueOrDefault() } equals new { k.Do_An_id, RapId = k.rap_id } into khoGroup
                         from k in khoGroup.DefaultIfEmpty()
                         select new ManagerInventoryViewModel
                         {
                             DoAn = d,
                             SoLuongTon = k != null ? (k.so_luong_ton ?? 0) : 0
                         };

            // Sắp xếp
            var model = result.OrderBy(x => x.DoAn.loai).ThenBy(x => x.DoAn.ten_san_pham).ToList();

            // Dropdown Categories
            ViewBag.Categories = db.Do_Ans
                .Where(d => d.loai != null)
                .Select(d => d.loai)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var cinemaStocks = db.Raps.Select(r => new CinemaStockViewModel
            {
                TenRap = r.ten_rap,
                // Tính tổng số lượng các món trong kho rạp này
                TongSoLuong = r.Kho_Do_Ans.Sum(k => (int?)k.so_luong_ton) ?? 0,

                // Lấy chi tiết từng món
                DanhSachMon = r.Kho_Do_Ans.Select(k => new StockItemViewModel
                {
                    TenMon = k.Do_An.ten_san_pham,
                    SoLuong = k.so_luong_ton ?? 0,
                    TrangThai = (k.so_luong_ton <= 0) ? "Hết hàng" :
                                (k.so_luong_ton < 20) ? "Sắp hết" : "Ổn định"
                }).OrderBy(m => m.SoLuong).ToList() // Sắp xếp món ít lên đầu để dễ thấy
            }).ToList();

            ViewBag.CinemaStocks = cinemaStocks;

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedCategory = category;

            return View(model);
        }
        // Trong InventoryManagementController.cs

        [HttpGet]
        public ActionResult GetStockDetail(int id)
        {
            var stocks = db.Kho_Do_Ans
                .Where(k => k.Do_An_id == id)
                .Select(k => new
                {
                    TenRap = k.Rap.ten_rap,
                    SoLuong = k.so_luong_ton ?? 0
                })
                .OrderBy(k => k.TenRap)
                .ToList();

            return Json(new { success = true, data = stocks }, JsonRequestBehavior.AllowGet);
        }
        // GET: Admin/InventoryManagement/Details/5
        public ActionResult Details(int id)
        {
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";

            int? currentRapId = GetCurrentRapId();

            var item = db.Do_Ans.FirstOrDefault(d => d.Do_An_id == id);
            if (item == null) return HttpNotFound();

            // 1. Lấy tồn kho hiện tại
            var khoItem = db.Kho_Do_Ans.FirstOrDefault(k => k.Do_An_id == id && k.rap_id == currentRapId);
            int stockQty = khoItem?.so_luong_ton ?? 0;

            // 2. Query cơ bản cho đơn hàng đã thanh toán
            var salesQuery = db.DonHang_DoAns
                .Where(dh => dh.Do_An_id == id
                             && dh.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán");

            // 3. Lọc theo Rạp (Quan trọng: Chỉ tính doanh số của rạp này)
            if (currentRapId.HasValue)
            {
                salesQuery = salesQuery.Where(dh => dh.Dat_Ve.Nhan_Vien.rap_id == currentRapId.Value);
            }

            // 4. Tính toán
            var totalSold = salesQuery.Sum(dh => (int?)dh.so_luong) ?? 0;

            var totalRevenue = salesQuery
                .Where(dh => dh.Do_An.gia.HasValue)
                .Sum(dh => (decimal?)(dh.so_luong * dh.Do_An.gia.Value)) ?? 0;

            var recentOrders = salesQuery
                .OrderByDescending(dh => dh.Dat_Ve.ngay_tao)
                .Take(10)
                .ToList();
            // 1. Lấy danh sách tồn kho của TẤT CẢ các rạp
            var stockByCinema = db.Kho_Do_Ans
                .Where(k => k.Do_An_id == id)
                .Select(k => new StockDetailViewModel
                {
                    TenRap = k.Rap.ten_rap,
                    SoLuongTon = k.so_luong_ton ?? 0
                })
                .OrderBy(k => k.TenRap)
                .ToList();

            // Nếu muốn hiển thị cả các rạp chưa có trong kho (số lượng 0)
            // Bạn có thể join với bảng Rap (Tùy chọn)

            ViewBag.StockByCinema = stockByCinema; // Truyền sang View

            ViewBag.StockQuantity = stockQty; // Thêm biến này
            ViewBag.TotalSold = totalSold;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.RecentOrders = recentOrders;

            return View(item);
        }

        // GET: Admin/InventoryManagement/Create
        // (Giữ nguyên logic tạo món mới, chỉ thêm status)
        public ActionResult Create()
        {
            SetupCategoryViewBag();
            return View();
        }

        // POST: Admin/InventoryManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Do_An item, HttpPostedFileBase imageFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (item.gia.HasValue && item.gia.Value < 0)
                    {
                        TempData["ErrorMessage"] = "Giá sản phẩm không được âm.";
                        SetupCategoryViewBag(item.loai);
                        return View(item);
                    }

                    // Set mặc định trạng thái
                    if (string.IsNullOrEmpty(item.trang_thai)) item.trang_thai = "Đang bán";

                    // Xử lý ảnh (nếu có logic upload ảnh)
                    // ...

                    db.Do_Ans.InsertOnSubmit(item);
                    db.SubmitChanges();

                    TempData["SuccessMessage"] = "Thêm món ăn vào Menu thành công!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

            SetupCategoryViewBag(item.loai);
            return View(item);
        }

        // GET: Admin/InventoryManagement/Edit/5
        public ActionResult Edit(int id)
        {
            var item = db.Do_Ans.FirstOrDefault(d => d.Do_An_id == id);
            if (item == null) return HttpNotFound();

            SetupCategoryViewBag(item.loai);
            return View(item);
        }

        // POST: Admin/InventoryManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, FormCollection form)
        {
            try
            {
                var item = db.Do_Ans.FirstOrDefault(d => d.Do_An_id == id);
                if (item == null) return HttpNotFound();

                item.ten_san_pham = form["ten_san_pham"];
                item.mo_ta = form["mo_ta"];
                item.loai = form["loai"];
                item.trang_thai = form["trang_thai"]; // Cập nhật trạng thái (Đang bán/Ngưng bán)

                if (decimal.TryParse(form["gia"], out decimal gia))
                {
                    if (gia < 0)
                    {
                        TempData["ErrorMessage"] = "Giá không hợp lệ.";
                        return RedirectToAction("Edit", new { id });
                    }
                    item.gia = gia;
                }

                db.SubmitChanges();
                TempData["SuccessMessage"] = "Cập nhật thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }
            return RedirectToAction("Edit", new { id });
        }

        // GET: Admin/InventoryManagement/Report
        // Báo cáo doanh thu (Đã lọc theo Rạp)
        public ActionResult Report(int? month, int? year)
        {
            int? currentRapId = GetCurrentRapId();
            if (currentRapId == null) return RedirectToAction("Index");

            int selectedYear = year ?? DateTime.Now.Year;
            int? selectedMonth = month;

            // Gọi Service đã viết ở bước trước, truyền vào RapId
            // Hàm GetTopSellingItems trong FoodService đã được sửa để nhận tham số rapId
            var topItems = _foodService.GetTopSellingItems(currentRapId, selectedMonth, selectedYear, 10);

            // Tính tổng doanh thu riêng cho rạp này
            // (Nếu FoodService chưa có hàm này, ta query trực tiếp ở đây cho nhanh)
            var salesQuery = db.DonHang_DoAns.Where(dh =>
                dh.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán" &&
                dh.Dat_Ve.Nhan_Vien.rap_id == currentRapId
            );

            if (selectedMonth.HasValue)
                salesQuery = salesQuery.Where(dh => dh.Dat_Ve.ngay_tao.Value.Month == selectedMonth && dh.Dat_Ve.ngay_tao.Value.Year == selectedYear);
            else
                salesQuery = salesQuery.Where(dh => dh.Dat_Ve.ngay_tao.Value.Year == selectedYear);

            var totalRevenue = salesQuery.Sum(dh => (decimal?)(dh.so_luong * (dh.Do_An.gia ?? 0))) ?? 0;
            var totalQuantity = salesQuery.Sum(dh => (int?)dh.so_luong) ?? 0;

            ViewBag.TopItems = topItems;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalQuantity = totalQuantity;
            ViewBag.PeriodLabel = selectedMonth.HasValue ? $"Tháng {selectedMonth}/{selectedYear}" : $"Năm {selectedYear}";
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;

            return View();
        }

        // GET: Admin/InventoryManagement/StockWarnings
        public ActionResult StockWarnings()
        {
            int? currentRapId = GetCurrentRapId();
            if (currentRapId == null) return RedirectToAction("Index");

            // Gọi Service lấy cảnh báo cho Rạp này
            var lowStockItems = _foodService.GetLowStockItems(currentRapId.Value, 20); // Dưới 20 là cảnh báo

            var outOfStock = lowStockItems.Where(x => x.SoLuongTon <= 0).ToList();
            var warningStock = lowStockItems.Where(x => x.SoLuongTon > 0).ToList();

            ViewBag.OutOfStock = outOfStock;
            ViewBag.LowStock = warningStock;

            return View();
        }

        // Helper private
        private void SetupCategoryViewBag(string selected = null)
        {
            ViewBag.Categories = new SelectList(new[]
            {
                "Đồ ăn", "Đồ uống", "Combo", "Snack", "Nước ngọt", "Nước ép", "Khác"
            }, selected);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
    public class StockDetailViewModel
    {
        public string TenRap { get; set; }
        public int SoLuongTon { get; set; }
    }
    public class CinemaStockViewModel
    {
        public string TenRap { get; set; }
        public int TongSoLuong { get; set; }
        public List<StockItemViewModel> DanhSachMon { get; set; }
    }

    public class StockItemViewModel
    {
        public string TenMon { get; set; }
        public int SoLuong { get; set; }
        public string TrangThai { get; set; } // "Hết hàng", "Sắp hết", "OK"
    }
}