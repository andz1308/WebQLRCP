using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Admin")]
    public class PromoController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/Promo
        public ActionResult Index(int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var promoCodes = db.Khuyen_Mais
                .OrderByDescending(k => k.ma_giam_gia_id)
                .ToList();

            int totalCount = promoCodes.Count;
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var pagedPromoCodes = promoCodes.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(pagedPromoCodes);
        }

        // GET: Admin/Promo/Create
        [HttpGet]
        public ActionResult Create()
        {
            // Lấy danh sách món ăn để hiển thị checkbox
            ViewBag.AllFoods = db.Do_Ans.Where(d => d.trang_thai == "Đang kinh doanh").ToList();
            return View();
        }

        // POST: Admin/Promo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection collection, int[] selectedFoods) // Thêm mảng selectedFoods
        {
            try
            {
                var khuyen_mai = new Khuyen_Mai();

                // Bind dữ liệu cơ bản
                khuyen_mai.ma_khuyen_mai = collection["ma_khuyen_mai"];
                khuyen_mai.mo_ta = collection["mo_ta"];

                if (decimal.TryParse(collection["gia_tri_giam"], out decimal giaTriGiam))
                    khuyen_mai.gia_tri_giam = giaTriGiam;

                khuyen_mai.loai_giam_gia = !string.IsNullOrEmpty(collection["loai_giam_gia"]) ? collection["loai_giam_gia"] : "Giảm giá";

                if (int.TryParse(collection["so_luong_con_lai"], out int soLuong))
                    khuyen_mai.so_luong_con_lai = soLuong;

                khuyen_mai.trang_thai = collection["trang_thai"] ?? "Hoạt động";

                if (DateTime.TryParse(collection["ngay_bat_dau"], out DateTime ngayBatDau))
                    khuyen_mai.ngay_bat_dau = ngayBatDau;

                if (DateTime.TryParse(collection["ngay_ket_thuc"], out DateTime ngayKetThuc))
                    khuyen_mai.ngay_ket_thuc = ngayKetThuc;

                // 🔴 CẬP NHẬT MỚI: PHẠM VI ÁP DỤNG
                // 0: Toàn đơn (Mặc định), 1: Theo món
                int phamVi = 0;
                if (int.TryParse(collection["pham_vi_ap_dung"], out phamVi))
                {
                    khuyen_mai.pham_vi_ap_dung = phamVi;
                }

                db.Khuyen_Mais.InsertOnSubmit(khuyen_mai);
                db.SubmitChanges(); // Lưu để lấy ID

                // 🔴 CẬP NHẬT MỚI: LƯU DANH SÁCH MÓN ĂN (Nếu phạm vi = 1)
                if (khuyen_mai.pham_vi_ap_dung == 1 && selectedFoods != null && selectedFoods.Length > 0)
                {
                    foreach (var foodId in selectedFoods)
                    {
                        var link = new Khuyen_Mai_Do_An
                        {
                            ma_giam_gia_id = khuyen_mai.ma_giam_gia_id,
                            do_an_id = foodId
                        };
                        db.Khuyen_Mai_Do_Ans.InsertOnSubmit(link);
                    }
                    db.SubmitChanges();
                }

                TempData["SuccessMessage"] = "Tạo mã khuyến mãi thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                ViewBag.ErrorMessage = "Lỗi: " + ex.Message;
                ViewBag.AllFoods = db.Do_Ans.Where(d => d.trang_thai == "Đang kinh doanh").ToList(); // Reload list nếu lỗi
                return View();
            }
        }

        // GET: Admin/Promo/Edit/{id}
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var khuyen_mai = db.Khuyen_Mais.FirstOrDefault(k => k.ma_giam_gia_id == id);
            if (khuyen_mai == null) return HttpNotFound();

            // Lấy danh sách món ăn & đánh dấu những món đã được chọn
            ViewBag.AllFoods = db.Do_Ans.Where(d => d.trang_thai == "Đang kinh doanh").ToList();

            // Lấy danh sách ID các món đã liên kết với mã này
            ViewBag.SelectedFoodIds = db.Khuyen_Mai_Do_Ans
                .Where(k => k.ma_giam_gia_id == id)
                .Select(k => k.do_an_id)
                .ToList();

            return View(khuyen_mai);
        }

        // POST: Admin/Promo/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, FormCollection collection, int[] selectedFoods)
        {
            try
            {
                var khuyen_mai = db.Khuyen_Mais.FirstOrDefault(k => k.ma_giam_gia_id == id);
                if (khuyen_mai == null) return HttpNotFound();

                // Update thông tin cơ bản
                khuyen_mai.mo_ta = collection["mo_ta"];

                if (decimal.TryParse(collection["gia_tri_giam"], out decimal giaTriGiam))
                    khuyen_mai.gia_tri_giam = giaTriGiam;

                if (int.TryParse(collection["so_luong_con_lai"], out int soLuong))
                    khuyen_mai.so_luong_con_lai = soLuong;

                khuyen_mai.trang_thai = collection["trang_thai"];

                if (DateTime.TryParse(collection["ngay_bat_dau"], out DateTime ngayBatDau))
                    khuyen_mai.ngay_bat_dau = ngayBatDau;

                if (DateTime.TryParse(collection["ngay_ket_thuc"], out DateTime ngayKetThuc))
                    khuyen_mai.ngay_ket_thuc = ngayKetThuc;

                // 🔴 CẬP NHẬT: PHẠM VI ÁP DỤNG
                if (int.TryParse(collection["pham_vi_ap_dung"], out int phamVi))
                {
                    khuyen_mai.pham_vi_ap_dung = phamVi;
                }

                // 🔴 CẬP NHẬT: DANH SÁCH MÓN ĂN
                // 1. Xóa hết liên kết cũ
                var oldLinks = db.Khuyen_Mai_Do_Ans.Where(k => k.ma_giam_gia_id == id).ToList();
                db.Khuyen_Mai_Do_Ans.DeleteAllOnSubmit(oldLinks);

                // 2. Thêm liên kết mới (Nếu phạm vi = 1)
                if (khuyen_mai.pham_vi_ap_dung == 1 && selectedFoods != null)
                {
                    foreach (var foodId in selectedFoods)
                    {
                        var link = new Khuyen_Mai_Do_An
                        {
                            ma_giam_gia_id = id,
                            do_an_id = foodId
                        };
                        db.Khuyen_Mai_Do_Ans.InsertOnSubmit(link);
                    }
                }

                db.SubmitChanges();
                TempData["SuccessMessage"] = "Cập nhật thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                ViewBag.ErrorMessage = "Lỗi: " + ex.Message;
                // Reload data for view
                ViewBag.AllFoods = db.Do_Ans.Where(d => d.trang_thai == "Đang kinh doanh").ToList();
                ViewBag.SelectedFoodIds = selectedFoods != null ? selectedFoods.ToList() : new List<int>();
                return View(db.Khuyen_Mais.FirstOrDefault(k => k.ma_giam_gia_id == id));
            }
        }

        // POST: Admin/Promo/Delete/{id} - Xóa mã khuyến mãi
        // POST: Admin/Promo/Delete/{id}
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                var khuyen_mai = db.Khuyen_Mais.FirstOrDefault(k => k.ma_giam_gia_id == id);
                if (khuyen_mai == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy mã khuyến mãi" });
                }

                // 🔴 LOGIC CŨ: Xóa hẳn khỏi DB
                // db.Khuyen_Mais.DeleteOnSubmit(khuyen_mai);

                // ✅ LOGIC MỚI: Chuyển trạng thái (Soft Delete)
                khuyen_mai.trang_thai = "Ngừng hoạt động";

                // Tùy chọn: Set số lượng về 0 để chắc chắn không ai dùng được nữa
                // khuyen_mai.so_luong_con_lai = 0; 

                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Đã ngừng hoạt động mã: {khuyen_mai.ma_khuyen_mai}");

                return Json(new { success = true, message = "Đã chuyển mã sang trạng thái 'Ngừng hoạt động'!" });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

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
