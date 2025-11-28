using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Infrastructure;
using WebCinema.Models;
using WebCinema.Services;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Staff")]
    public class StaffPurchaseOrderController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();
        private FoodService _foodService = new FoodService();

        // Helper lấy thông tin nhân viên
        private Nhan_Vien GetCurrentStaff()
        {
            var username = User.Identity.Name;
            return db.Nhan_Viens.FirstOrDefault(nv => nv.email == username || nv.ho_ten == username);
        }

        // 1. Danh sách phiếu nhập (Lịch sử)
        public ActionResult Index()
        {
            var staff = GetCurrentStaff();
            if (staff == null || staff.rap_id == null) return RedirectToAction("Login", "Account");

            // Lấy danh sách phiếu nhập của Rạp này
            var list = db.Phieu_Nhaps
                .Where(p => p.rap_id == staff.rap_id)
                .OrderByDescending(p => p.ngay_nhap)
                .ToList();

            return View(list);
        }

        // 2. Trang tạo phiếu nhập (Giao diện chọn món)
        public ActionResult Create()
        {
            // Lấy danh sách đồ ăn để hiển thị vào dropdown
            ViewBag.ListFood = db.Do_Ans.Where(d => d.trang_thai != "Ngừng kinh doanh").ToList();
            return View();
        }

        // 3. Xử lý lưu phiếu nhập (Nhận JSON từ View qua Ajax)
        [HttpPost]
        public JsonResult Create(List<ChiTietNhapModel> items, string ghiChu)
        {
            var staff = GetCurrentStaff();
            if (staff == null) return Json(new { success = false, message = "Vui lòng đăng nhập lại." });

            if (items == null || items.Count == 0)
                return Json(new { success = false, message = "Chưa chọn món ăn nào." });

            // Convert model tạm sang entity
            var listEntity = items.Select(x => new Chi_Tiet_Phieu_Nhap
            {
                do_an_id = x.Id,
                so_luong_nhap = x.SoLuong
            }).ToList();

            bool result = _foodService.CreateImportRequest(staff.nhanvien_id, staff.rap_id.Value, listEntity, ghiChu);

            if (result)
                return Json(new { success = true, message = "Tạo yêu cầu nhập hàng thành công!" });
            else
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo phiếu." });
        }

        // 4. Xác nhận nhập kho (Bước 3 của quy trình)
        [HttpPost]
        public JsonResult ConfirmReceipt(int id)
        {
            var staff = GetCurrentStaff();
            string result = _foodService.ConfirmReceipt(id, staff.nhanvien_id);

            if (result == "Success")
                return Json(new { success = true });
            else
                return Json(new { success = false, message = result });
        }
    }

    // Class phụ để nhận dữ liệu JSON
    public class ChiTietNhapModel
    {
        public int Id { get; set; }
        public int SoLuong { get; set; }
    }
}