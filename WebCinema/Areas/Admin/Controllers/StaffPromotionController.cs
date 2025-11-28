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
        public ActionResult Create()
        {
            var staff = GetCurrentStaff();
            ViewBag.SlowItems = _foodService.GetSlowSellingItems(staff.rap_id.Value);
            ViewBag.AllFoods = db.Do_Ans.Where(d => d.trang_thai == "Đang kinh doanh").ToList();
            return View();
        }

        // 4. Xử lý lưu (Ajax POST)
        [HttpPost]
        public JsonResult Create(List<ProposalItemModel> items, string reason)
        {
            var staff = GetCurrentStaff();
            if (items == null || items.Count == 0) return Json(new { success = false, message = "Chưa chọn món nào." });

            var details = items.Select(x => new Chi_Tiet_De_Xuat_Khuyen_Mai { do_an_id = x.Id, muc_giam_gia = x.Percent }).ToList();
            bool result = _foodService.CreatePromotionProposal(staff.nhanvien_id, staff.rap_id.Value, reason, details);

            return Json(new { success = result });
        }
    }

    public class ProposalItemModel { public int Id { get; set; } public int Percent { get; set; } }
    // Paste cái này vào cuối file StaffPromotionController.cs hoặc tạo file mới trong Models
    public class PromotionDetailViewModel
    {
        public WebCinema.Models.Chi_Tiet_De_Xuat_Khuyen_Mai ct { get; set; }
        public WebCinema.Models.Do_An da { get; set; }
    }
}