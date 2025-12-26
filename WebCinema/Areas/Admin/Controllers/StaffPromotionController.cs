using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Infrastructure;
using WebCinema.Models;
using WebCinema.Services;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Staff")]
    public class StaffPromotionController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();
        private FoodService _foodService = new FoodService();

        private Nhan_Vien GetCurrentStaff()
        {
            var username = User.Identity.Name;
            return db.Nhan_Viens.FirstOrDefault(nv => nv.email == username || nv.ho_ten == username);
        }

        // 1. Danh sách lịch sử
        public ActionResult Index()
        {
            var staff = GetCurrentStaff();
            if (staff == null || staff.rap_id == null) return RedirectToAction("Login", "Account");

            var list = db.Phieu_De_Xuat_Khuyen_Mais
                .Where(p => p.rap_id == staff.rap_id)
                .OrderByDescending(p => p.ngay_tao).ToList();
            return View(list);
        }

        // 2. Xem chi tiết phiếu
        public ActionResult Details(int id)
        {
            var phieu = db.Phieu_De_Xuat_Khuyen_Mais.FirstOrDefault(p => p.de_xuat_id == id);
            if (phieu == null) return HttpNotFound();

            // Join để lấy tên món ăn
            var details = from ct in db.Chi_Tiet_De_Xuat_Khuyen_Mais
                          join da in db.Do_Ans on ct.do_an_id equals da.Do_An_id
                          where ct.de_xuat_id == id
                          // Thay vì new { ct, da } (Anonymous Type)
                          // Hãy dùng class cụ thể vừa tạo:
                          select new PromotionDetailViewModel
                          {
                              ct = ct,
                              da = da
                          };
            ViewBag.Details = details.ToList();
            return View(phieu);
        }

        // 3. Trang tạo phiếu mới (Kèm gợi ý)
        // GET: Admin/StaffPromotion/Create
        public ActionResult Create(string autoMode, int? minStock)
        {
            var staff = GetCurrentStaff();
            if (staff == null) return RedirectToAction("Login");

            int stockLimit = minStock ?? 0;
            ViewBag.StockLimit = stockLimit; // <--- Truyền sang View để giữ giá trị này

            // Lấy danh sách tất cả món ăn để hiển thị dropdown thủ công
            ViewBag.AllFoods = db.Do_Ans.Where(d => d.trang_thai == "Đang kinh doanh").ToList();

            // 🆕 LOGIC MỚI: CHỈ LỌC THEO TỒN KHO (BỎ QUA DOANH SỐ BÁN)
            if (autoMode == "SlowStock" && staff.rap_id.HasValue)
            {
                int limit = minStock ?? 50; // Mặc định 50 nếu không nhập

                // Tìm các món có tồn kho >= limit
                var highStockItems = db.Kho_Do_Ans
                    .Where(k => k.rap_id == staff.rap_id.Value && k.so_luong_ton >= limit)
                    .Select(k => new {
                        Id = k.Do_An_id,
                        Name = k.Do_An.ten_san_pham,
                        CurrentStock = k.so_luong_ton
                    })
                    .ToList();

                if (highStockItems.Any())
                {
                    var autoPromoList = highStockItems.Select(x => new {
                        Id = x.Id,
                        Name = x.Name,
                        Percent = 20 // Mặc định giảm 20%
                    }).ToList();

                    ViewBag.AutoPromoList = Newtonsoft.Json.JsonConvert.SerializeObject(autoPromoList);
                    ViewBag.AutoMessage = $"Đã tìm thấy {highStockItems.Count} món có tồn kho cao (>= {limit}).";

                    // Không cần ViewBag.SlowItems nữa vì ta dùng AutoPromoList JSON trực tiếp
                }
                else
                {
                    ViewBag.AutoMessage = $"Không có món nào tồn kho trên {limit}.";
                }
            }

            return View();
        }

        // 4. Xử lý lưu (Ajax POST)
        [HttpPost]
        public JsonResult Create(List<ProposalItemSimpleModel> items, string reason, int percent, int stockThreshold)
        {
            var staff = GetCurrentStaff();
            if (items == null || items.Count == 0) return Json(new { success = false, message = "Chưa chọn món nào." });

            var details = items.Select(x => new Chi_Tiet_De_Xuat_Khuyen_Mai { do_an_id = x.Id }).ToList();

            // Gọi Service
            bool result = _foodService.CreatePromotionProposal(staff.nhanvien_id, staff.rap_id.Value, reason, percent, stockThreshold, details);

            if (result) return Json(new { success = true, message = "Gửi đề xuất thành công!" });
            return Json(new { success = false, message = "Lỗi hệ thống." });
        }
    }

    public class ProposalItemModel { public int Id { get; set; } public int Percent { get; set; } }
    // Paste cái này vào cuối file StaffPromotionController.cs hoặc tạo file mới trong Models
    public class PromotionDetailViewModel
    {
        public WebCinema.Models.Chi_Tiet_De_Xuat_Khuyen_Mai ct { get; set; }
        public WebCinema.Models.Do_An da { get; set; }
    }
    public class ProposalItemSimpleModel
    {
        public int Id { get; set; }
        // public string Name { get; set; } // Có thể thêm name để hiển thị lại nếu cần
    }
}