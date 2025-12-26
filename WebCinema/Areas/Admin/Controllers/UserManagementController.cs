using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;
using System.Security.Cryptography;
using System.Text;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/UserManagement
        public ActionResult Index(string searchTerm, string role, int? page)
        {
            int pageSize = 10; // 10 items per page
            int pageNumber = page ?? 1;

            var users = db.Khach_Hangs.AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                users = users.Where(u => u.ho_ten.Contains(searchTerm) || 
                                        u.email.Contains(searchTerm) || 
                                        u.so_dien_thoai.Contains(searchTerm));
            }

            var orderedUsers = users.OrderByDescending(u => u.ngay_dang_ky);

            // Calculate pagination
            int totalItems = orderedUsers.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var result = orderedUsers
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(result);
        }

        // GET: Admin/UserManagement/Details/5
        public ActionResult Details(int id)
        {
            var user = db.Khach_Hangs.FirstOrDefault(u => u.khach_hang_id == id);
            if (user == null)
            {
                return HttpNotFound();
            }

            ViewBag.TotalBookings = user.Dat_Ves.Count();
            ViewBag.TotalSpent = user.Dat_Ves.Sum(d => (decimal?)d.tong_tien) ?? 0;
            ViewBag.RecentBookings = user.Dat_Ves.OrderByDescending(d => d.ngay_tao).Take(5).ToList();

            return View(user);
        }

        // GET: Admin/UserManagement/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Khach_Hang user, string password)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Check if email already exists
                    if (db.Khach_Hangs.Any(u => u.email == user.email))
                    {
                        ModelState.AddModelError("email", "Email đã tồn tại.");
                        return View(user);
                    }

                    // Hash password
                    user.mat_khau = HashPassword(password);
                    user.ngay_dang_ky = DateTime.Now;

                    db.Khach_Hangs.InsertOnSubmit(user);
                    db.SubmitChanges();

                    TempData["SuccessMessage"] = "Thêm người dùng thành công!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return View(user);
        }

        // GET: Admin/UserManagement/Edit/5
        public ActionResult Edit(int id)
        {
            var user = db.Khach_Hangs.FirstOrDefault(u => u.khach_hang_id == id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Admin/UserManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, FormCollection form)
        {
            try
            {
                var user = db.Khach_Hangs.FirstOrDefault(u => u.khach_hang_id == id);
                if (user == null)
                {
                    return HttpNotFound();
                }

                user.ho_ten = form["ho_ten"];
                user.email = form["email"];
                user.so_dien_thoai = form["so_dien_thoai"];
                
                // ✅ Cập nhật các cột mới
                if (!string.IsNullOrEmpty(form["ngay_sinh"]))
                {
                    user.ngay_sinh = DateTime.Parse(form["ngay_sinh"]);
                }
                user.gioi_tinh = form["gioi_tinh"];
                user.dia_chi = form["dia_chi"];

                // Update password if provided
                if (!string.IsNullOrEmpty(form["password"]))
                {
                    user.mat_khau = HashPassword(form["password"]);
                }

                db.SubmitChanges();

                TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Edit", new { id });
        }

        // POST: Admin/UserManagement/Delete/5
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                var user = db.Khach_Hangs.FirstOrDefault(u => u.khach_hang_id == id);
                if (user == null)
                {
                    return Json(new { success = false, message = "Người dùng không tồn tại." });
                }

                // Check if user has bookings
                if (user.Dat_Ves.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa người dùng đã có đơn đặt vé." });
                }

                db.Khach_Hangs.DeleteOnSubmit(user);
                db.SubmitChanges();

                return Json(new { success = true, message = "Xóa người dùng thành công!" });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // ===== STAFF MANAGEMENT =====

        // GET: Admin/UserManagement/Staff
        public ActionResult Staff(int? page)
        {
            int pageSize = 10; // 10 items per page
            int pageNumber = page ?? 1;

            var staffQuery = db.Nhan_Viens.OrderByDescending(nv => nv.ngay_vao_lam);

            // Calculate pagination
            int totalItems = staffQuery.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var staff = staffQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(staff);
        }

        // GET: Admin/UserManagement/CreateStaff
        public ActionResult CreateStaff()
        {
            // ✅ CHỈ LẤY 2 ROLE: Admin và Staff
            var allowedRoles = new[] { "Admin", "Staff" };
            ViewBag.Roles = new SelectList(
                db.Roles.Where(r => allowedRoles.Contains(r.ten_role)).ToList(), 
                "role_id", 
                "ten_role"
            );
            ViewBag.Raps = new SelectList(db.Raps.OrderBy(r => r.ten_rap), "rap_id", "ten_rap");
            return View();
        }

        // POST: Admin/UserManagement/CreateStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStaff(Nhan_Vien staff, string password)
        {
            try
            {
                // ✅ VALIDATION: Kiểm tra ngày sinh
                if (staff.ngay_sinh.HasValue)
                {
                    var today = DateTime.Today;
                    var age = today.Year - staff.ngay_sinh.Value.Year;
                    
                    // Kiểm tra nếu chưa đến sinh nhật năm nay thì trừ 1 tuổi
                    if (staff.ngay_sinh.Value.Date > today.AddYears(-age)) age--;
                    
                    // ✅ Kiểm tra tuổi phải >= 18
                    if (age < 18)
                    {
                        ModelState.AddModelError("ngay_sinh", "Nhân viên phải từ 18 tuổi trở lên.");
                    }
                    
                    // ✅ Kiểm tra không được chọn ngày tương lai
                    if (staff.ngay_sinh.Value.Date > today)
                    {
                        ModelState.AddModelError("ngay_sinh", "Ngày sinh không được ở tương lai.");
                    }
                }

                if (ModelState.IsValid)
                {
                    // Check if email already exists
                    if (db.Nhan_Viens.Any(nv => nv.email == staff.email))
                    {
                        ModelState.AddModelError("email", "Email đã tồn tại.");
                        
                        // ✅ CHỈ LẤY 2 ROLE
                        var allowedRoles = new[] { "Admin", "Staff" };
                        ViewBag.Roles = new SelectList(
                            db.Roles.Where(r => allowedRoles.Contains(r.ten_role)).ToList(), 
                            "role_id", 
                            "ten_role", 
                            staff.role_id
                        );
                        ViewBag.Raps = new SelectList(db.Raps.OrderBy(r => r.ten_rap), "rap_id", "ten_rap", staff.rap_id);
                        return View(staff);
                    }

                    staff.mat_khau = HashPassword(password);
                    staff.ngay_vao_lam = DateTime.Now;
                    staff.trang_thai = "Hoạt động";

                    db.Nhan_Viens.InsertOnSubmit(staff);
                    db.SubmitChanges();

                    TempData["SuccessMessage"] = "Thêm nhân viên thành công!";
                    return RedirectToAction("Staff");
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            // ✅ CHỈ LẤY 2 ROLE khi có lỗi
            var allowedRolesError = new[] { "Admin", "Staff" };
            ViewBag.Roles = new SelectList(
                db.Roles.Where(r => allowedRolesError.Contains(r.ten_role)).ToList(), 
                "role_id", 
                "ten_role", 
                staff.role_id
            );
            ViewBag.Raps = new SelectList(db.Raps.OrderBy(r => r.ten_rap), "rap_id", "ten_rap", staff.rap_id);
            return View(staff);
        }

        // GET: Admin/UserManagement/EditStaff/5
        public ActionResult EditStaff(int id)
        {
            var staff = db.Nhan_Viens.FirstOrDefault(nv => nv.nhanvien_id == id);
            if (staff == null)
            {
                return HttpNotFound();
            }
            
            // ✅ CHỈ LẤY 2 ROLE
            var allowedRoles = new[] { "Admin", "Staff" };
            ViewBag.Roles = new SelectList(
                db.Roles.Where(r => allowedRoles.Contains(r.ten_role)).ToList(), 
                "role_id", 
                "ten_role", 
                staff.role_id
            );
            ViewBag.Raps = new SelectList(db.Raps.OrderBy(r => r.ten_rap), "rap_id", "ten_rap", staff.rap_id);
            return View(staff);
        }

        // POST: Admin/UserManagement/EditStaff/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStaff(int id, FormCollection form)
        {
            try
            {
                var staff = db.Nhan_Viens.FirstOrDefault(nv => nv.nhanvien_id == id);
                if (staff == null)
                {
                    return HttpNotFound();
                }

                // ✅ VALIDATION: Kiểm tra ngày sinh
                if (!string.IsNullOrEmpty(form["ngay_sinh"]))
                {
                    DateTime ngaySinh = DateTime.Parse(form["ngay_sinh"]);
                    var today = DateTime.Today;
                    var age = today.Year - ngaySinh.Year;
                    
                    // Kiểm tra nếu chưa đến sinh nhật năm nay thì trừ 1 tuổi
                    if (ngaySinh.Date > today.AddYears(-age)) age--;
                    
                    // ✅ Kiểm tra tuổi phải >= 18
                    if (age < 18)
                    {
                        TempData["ErrorMessage"] = "Nhân viên phải từ 18 tuổi trở lên.";
                        return RedirectToAction("EditStaff", new { id });
                    }
                    
                    // ✅ Kiểm tra không được chọn ngày tương lai
                    if (ngaySinh.Date > today)
                    {
                        TempData["ErrorMessage"] = "Ngày sinh không được ở tương lai.";
                        return RedirectToAction("EditStaff", new { id });
                    }
                    
                    staff.ngay_sinh = ngaySinh;
                }

                staff.ho_ten = form["ho_ten"];
                staff.email = form["email"];
                staff.so_dien_thoai = form["so_dien_thoai"];
                staff.gioi_tinh = form["gioi_tinh"];
                staff.trang_thai = form["trang_thai"];
                staff.dia_chi = form["dia_chi"];

                if (!string.IsNullOrEmpty(form["role_id"]))
                {
                    staff.role_id = int.Parse(form["role_id"]);
                }

                // ✅ Xử lý rap_id (NULL cho Admin, có giá trị cho Staff)
                if (!string.IsNullOrEmpty(form["rap_id"]))
                {
                    int rapIdValue = 0;
                    if (int.TryParse(form["rap_id"], out rapIdValue) && rapIdValue > 0)
                    {
                        staff.rap_id = rapIdValue;
                    }
                    else
                    {
                        staff.rap_id = null; // Admin không gán rạp
                    }
                }
                else
                {
                    staff.rap_id = null; // Admin không gán rạp
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(form["password"]))
                {
                    staff.mat_khau = HashPassword(form["password"]);
                }

                db.SubmitChanges();

                TempData["SuccessMessage"] = "Cập nhật nhân viên thành công!";
                return RedirectToAction("Staff");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("EditStaff", new { id });
        }

        // POST: Admin/UserManagement/DeleteStaff/5
        [HttpPost]
        public ActionResult DeleteStaff(int id)
        {
            try
            {
                var staff = db.Nhan_Viens.FirstOrDefault(nv => nv.nhanvien_id == id);
                if (staff == null)
                {
                    return Json(new { success = false, message = "Nhân viên không tồn tại." });
                }

                // Check if staff has related bookings
                if (staff.Dat_Ves.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa nhân viên đã xử lý đơn đặt vé." });
                }

                db.Nhan_Viens.DeleteOnSubmit(staff);
                db.SubmitChanges();

                return Json(new { success = true, message = "Xóa nhân viên thành công!" });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
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
}
