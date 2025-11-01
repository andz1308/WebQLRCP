using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models; // (Hoặc nơi bạn định nghĩa CSDLDataContext)
using WebCinema.Infrastructure; // (Nơi chứa RoleAuthorize)
using System.Collections.Generic; // Để dùng List<>

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Admin")]
    public class AuditoriController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/PhongChieu
        public ActionResult Index()
        {
            // Lấy danh sách phòng chiếu, kèm thông tin Rạp
            var phongChieus = db.Phong_Chieus.OrderBy(p => p.Rap.ten_rap).ThenBy(p => p.ten_phong).ToList();
            return View(phongChieus);
        }

        // GET: Admin/PhongChieu/Create
        public ActionResult Create(int? rapId)
        {
            var model = new Phong_Chieu();

            if (rapId == null)
            {
                // 1. Trường hợp cũ: Tạo mà không có rapId (vẫn hỗ trợ)
                ViewBag.RapList = new SelectList(db.Raps, "rap_id", "ten_rap");
                ViewBag.HasSpecificRap = false;
            }
            else
            {
                // 2. Trường hợp mới: Đã biết rạp
                var rap = db.Raps.SingleOrDefault(r => r.rap_id == rapId);
                if (rap == null)
                {
                    return HttpNotFound("Không tìm thấy rạp.");
                }

                // Gán rap_id cho model và gửi thông tin rạp qua ViewBag
                model.rap_id = rapId.Value;
                ViewBag.RapName = rap.ten_rap;
                ViewBag.HasSpecificRap = true;
                ViewBag.RapList = null; // Không cần danh sách
            }

            return View(model);
        }

        // POST: Admin/PhongChieu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Sửa đổi: Truyền model 'phongChieu'
        public ActionResult Create(Phong_Chieu phongChieu)
        {
            // Kiểm tra xem rap_id có hợp lệ không (phòng trường hợp người dùng cố tình sửa hidden field)
            if (phongChieu.rap_id == 0)
            {
                ModelState.AddModelError("rap_id", "Vui lòng chọn một rạp chiếu.");
            }

            if (ModelState.IsValid)
            {
                // ... (Toàn bộ logic tạo ghế y như cũ) ...
                db.Phong_Chieus.InsertOnSubmit(phongChieu);
                db.SubmitChanges();

                List<Ghe>
    gheMoiList = new List<Ghe>
        ();
                int defaultLoaiGheId = db.Loai_Ghes.FirstOrDefault(lg => lg.ten_loai == "Standard").loaighe_id;

                for (int i = 0; i < phongChieu.so_hang; i++)
                {
                    char hangChu = (char)('A' + i);
                    for (int j = 0; j < phongChieu.so_cot; j++)
                    {
                        string soGhe = $"{hangChu}{j + 1}";
                        // ... (tạo gheMoi) ...
                        Ghe gheMoi = new Ghe
                        {
                            phong_chieu_id = phongChieu.phong_chieu_id,
                            hang = i,
                            cot = j,
                            trang_thai = 2,
                            so_ghe = soGhe,
                            loai_ghe_id = defaultLoaiGheId
                        };
                        gheMoiList.Add(gheMoi);
                    }
                }

                db.Ghes.InsertAllOnSubmit(gheMoiList);
                db.SubmitChanges();

                return RedirectToAction("EditLayout", new { id = phongChieu.phong_chieu_id });
            }

            // Nếu thất bại, tải lại ViewBags cho đúng
            if (ViewBag.HasSpecificRap == true || phongChieu.rap_id != 0)
            {
                var rap = db.Raps.SingleOrDefault(r => r.rap_id == phongChieu.rap_id);
                ViewBag.RapName = rap?.ten_rap ?? "Lỗi";
                ViewBag.HasSpecificRap = true;
                ViewBag.RapList = null;
            }
            else
            {
                ViewBag.RapList = new SelectList(db.Raps, "rap_id", "ten_rap", phongChieu.rap_id);
                ViewBag.HasSpecificRap = false;
            }

            return View(phongChieu);
        }

        // GET: Admin/PhongChieu/EditLayout/5
        public ActionResult EditLayout(int id)
        {
            var phong = db.Phong_Chieus.SingleOrDefault(p => p.phong_chieu_id == id);
            if (phong == null) return HttpNotFound();

            // Lấy danh sách ghế theo đúng thứ tự hàng, cột
            var gheList = db.Ghes.Where(g => g.phong_chieu_id == id)
            .OrderBy(g => g.hang)
            .ThenBy(g => g.cot)
            .ToList();

            var loaiGheList = db.Loai_Ghes.ToList();

            // Tạo ViewModel (hoặc dùng ViewBag) để truyền dữ liệu
            // Tạm dùng ViewBag cho đơn giản theo phong cách của bạn
            ViewBag.PhongChieu = phong;
            ViewBag.LoaiGheList = loaiGheList;

            return View(gheList); // Truyền danh sách ghế làm Model chính
        }

        // POST: Admin/PhongChieu/UpdateGheDetails (Dùng cho AJAX)
        [HttpPost]
        public JsonResult UpdateGheDetails(int ghe_id, int trang_thai, string so_ghe, int loai_ghe_id)
        {
            try
            {
                // 1. Tìm ghế
                Ghe ghe = db.Ghes.SingleOrDefault(g => g.ghe_id == ghe_id);
                if (ghe == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy ghế!" });
                }

                // 2. Cập nhật thông tin
                ghe.trang_thai = trang_thai;
                ghe.so_ghe = so_ghe;
                ghe.loai_ghe_id = loai_ghe_id;

                // 3. Lưu thay đổi
                db.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/PhongChieu/UpdateCapacity (Nhấn nút "Hoàn tất")
        [HttpPost]
        public ActionResult UpdateCapacity(int phong_chieu_id)
        {
            var phong = db.Phong_Chieus.SingleOrDefault(p => p.phong_chieu_id == phong_chieu_id);
            if (phong == null) return HttpNotFound();

            // Đếm tất cả các ghế CÓ THỂ NGỒI ĐƯỢC (trạng thái 0)
            int soLuongGhe = db.Ghes.Count(g =>
            g.phong_chieu_id == phong_chieu_id &&
            g.trang_thai == 0 // 0 = Ghế
            );

            phong.suc_chua = soLuongGhe;
            db.SubmitChanges();

            return RedirectToAction("Index"); // Về trang danh sách phòng
        }

        // (Thêm các hàm Edit, Delete cho Phòng Chiếu nếu cần)

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
