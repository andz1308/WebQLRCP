using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebCinema.Models;
using WebCinema.Services;

namespace WebCinema.Controllers
{
    public class AccountController : Controller
    {
        private AuthService authService = new AuthService();

        // GET: Account/Login
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var auth = authService.Login(model.Email, model.Password);

                if (auth != null && auth.IsAuthenticated)
                {
                    // Clear any previous session
                    Session.Clear();

                    if (auth.Role == "Customer" && auth.Customer != null)
                    {
                        Session["CustomerId"] = auth.Customer.khach_hang_id;
                        Session["CustomerName"] = auth.Customer.ho_ten;
                        Session["CustomerEmail"] = auth.Customer.email;
                        Session["UserRole"] = "Customer";

                        if (model.RememberMe)
                        {
                            // Custom lightweight cookie for UI (optional)
                            HttpCookie userCookie = new HttpCookie("UserAuth");
                            userCookie["Email"] = auth.Customer.email;
                            userCookie.Expires = DateTime.Now.AddDays(30);
                            userCookie.Path = "/";
                            Response.Cookies.Add(userCookie);

                            // Create persistent forms auth cookie (explicit ticket) so authentication persists
                            var ticket = new FormsAuthenticationTicket(
                                1,
                                auth.Customer.email,
                                DateTime.Now,
                                DateTime.Now.AddDays(30),
                                true,
                                "Customer",
                                FormsAuthentication.FormsCookiePath);

                            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
                            var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                            {
                                HttpOnly = true,
                                Expires = ticket.Expiration,
                                Path = FormsAuthentication.FormsCookiePath,
                                Secure = Request.IsSecureConnection
                            };

                            Response.Cookies.Add(authCookie);
                        }
                        else
                        {
                            // Non-persistent auth cookie for session-only
                            FormsAuthentication.SetAuthCookie(auth.Customer.email, false);
                        }
                    }
                    else if ((auth.Role == "Admin" || auth.Role == "Staff") && auth.Employee != null)
                    {
                        Session["EmployeeId"] = auth.Employee.nhanvien_id;
                        Session["EmployeeName"] = auth.Employee.ho_ten;
                        Session["EmployeeEmail"] = auth.Employee.email;
                        Session["UserRole"] = auth.Role; // Admin or Staff

                        if (model.RememberMe)
                        {
                            HttpCookie userCookie = new HttpCookie("UserAuth");
                            userCookie["Email"] = auth.Employee.email;
                            userCookie.Expires = DateTime.Now.AddDays(30);
                            userCookie.Path = "/";
                            Response.Cookies.Add(userCookie);

                            var ticket = new FormsAuthenticationTicket(
                                1,
                                auth.Employee.email,
                                DateTime.Now,
                                DateTime.Now.AddDays(30),
                                true,
                                auth.Role,
                                FormsAuthentication.FormsCookiePath);

                            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
                            var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                            {
                                HttpOnly = true,
                                Expires = ticket.Expiration,
                                Path = FormsAuthentication.FormsCookiePath,
                                Secure = Request.IsSecureConnection
                            };

                            Response.Cookies.Add(authCookie);
                        }
                        else
                        {
                            FormsAuthentication.SetAuthCookie(auth.Employee.email, false);
                        }
                    }

                    // Redirect based on role
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    if (auth.Role == "Admin")
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    }

                    if (auth.Role == "Staff")
                    {
                        return RedirectToAction("Index", "StaffDashboardNew", new { area = "Admin" });
                    }

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                string errorMessage;
                bool success = authService.Register(model, out errorMessage);

                if (success)
                {
                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("", errorMessage);
            }

            return View(model);
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            // Sign out forms auth
            try
            {
                FormsAuthentication.SignOut();

                // Remove forms auth cookie
                if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
                {
                    var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, "");
                    cookie.Expires = DateTime.Now.AddDays(-1);
                    cookie.Path = FormsAuthentication.FormsCookiePath;
                    Response.Cookies.Add(cookie);
                }
            }
            catch
            {
                // ignore
            }

            // Clear session
            Session.Clear();
            Session.Abandon();

            // Clear custom cookie
            if (Request.Cookies["UserAuth"] != null)
            {
                HttpCookie userCookie = new HttpCookie("UserAuth");
                userCookie.Expires = DateTime.Now.AddDays(-1);
                userCookie.Path = "/";
                Response.Cookies.Add(userCookie);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Profile
        new public ActionResult Profile()
        {
            // If customer
            if (Session["UserRole"] == null)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("Profile") });
            }

            var role = Session["UserRole"].ToString();

            if (role == "Customer")
            {
                int customerId = (int)Session["CustomerId"];
                var customer = authService.GetCustomerById(customerId);

                if (customer == null)
                {
                    return RedirectToAction("Logout");
                }

                // ✅ Lấy lịch sử đặt vé - CHỈ NHỮNG ĐƠN ĐÃ THANH TOÁN
                var db = new CSDLDataContext();
                var bookings = db.Dat_Ves
                    .Where(b => b.khach_hang_id == customerId && b.trang_thai_Dat_Ve == "Đã Thanh toán")
                    .OrderByDescending(b => b.ngay_tao)
                    .ToList()  // ✅ Chuyển sang LINQ to Objects trước
                    .Select(b => new CustomerBookingViewModel
                    {
                        booking_id = b.Dat_Ve_id,
                        movie_name = b.Ves.FirstOrDefault() != null ? b.Ves.FirstOrDefault().Suat_Chieu.Phim.ten_phim : "N/A",
                        show_date = b.Ves.FirstOrDefault() != null ? b.Ves.FirstOrDefault().Suat_Chieu.ngay_chieu.ToString("dd/MM/yyyy") : "N/A",
                        cinema_name = b.Ves.FirstOrDefault() != null ? b.Ves.FirstOrDefault().Suat_Chieu.Phong_Chieu.Rap.ten_rap : "N/A",
                        ticket_count = b.Ves.Count,
                        total_amount = b.tong_tien,
                        status = b.trang_thai_Dat_Ve
                    })
                    .ToList();

                ViewBag.Bookings = bookings;

                // ✅ TÍNH ĐIỂM: chỉ tính từ các đơn "Đã Thanh toán" (1 điểm = 1 vé đã thanh toán)
                var pointsFromPaidBookings = db.Dat_Ves
                    .Where(b => b.khach_hang_id == customerId && b.trang_thai_Dat_Ve == "Đã Thanh toán")
                    .SelectMany(b => b.Ves)
                    .Count();

                ViewBag.CalculatedPoints = pointsFromPaidBookings;

                return View(customer);
            }

            // For staff/admin show employee profile view (reuse same view or create new)
            if (role == "Admin" || role == "Staff")
            {
                int employeeId = (int)Session["EmployeeId"];
                var employee = authService.GetEmployeeById(employeeId);
                if (employee == null)
                {
                    return RedirectToAction("Logout");
                }

                return View("ProfileEmployee", employee);
            }

            return RedirectToAction("Logout");
        }

        // ✅ GET: Account/EditProfile
        [HttpGet]
        public ActionResult EditProfile()
        {
            if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Customer")
            {
                return RedirectToAction("Login");
            }

            int customerId = (int)Session["CustomerId"];
            var customer = authService.GetCustomerById(customerId);

            if (customer == null)
            {
                return RedirectToAction("Logout");
            }

            return View(customer);
        }

        // ✅ POST: Account/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(Khach_Hang model)
        {
            try
            {
                if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Customer")
                {
                    return RedirectToAction("Login");
                }

                int customerId = (int)Session["CustomerId"];
                var db = new CSDLDataContext();
                var customer = db.Khach_Hangs.FirstOrDefault(k => k.khach_hang_id == customerId);

                if (customer == null)
                {
                    return RedirectToAction("Logout");
                }

                // ✅ Cập nhật thông tin
                customer.ho_ten = model.ho_ten;
                customer.so_dien_thoai = model.so_dien_thoai;
                customer.ngay_sinh = model.ngay_sinh;
                customer.gioi_tinh = model.gioi_tinh;
                customer.dia_chi = model.dia_chi;

                db.SubmitChanges();

                // ✅ Cập nhật session
                Session["CustomerName"] = customer.ho_ten;

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return View(model);
            }
        }

        // ✅ GET: Account/ChangePassword
        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Customer")
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // ✅ POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            try
            {
                if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Customer")
                {
                    return RedirectToAction("Login");
                }

                // Validation
                if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
                {
                    TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin!";
                    return View();
                }

                if (NewPassword != ConfirmPassword)
                {
                    TempData["ErrorMessage"] = "Mật khẩu xác nhận không trùng khớp!";
                    return View();
                }

                if (NewPassword.Length < 8)
                {
                    TempData["ErrorMessage"] = "Mật khẩu mới phải tối thiểu 8 ký tự!";
                    return View();
                }

                if (CurrentPassword == NewPassword)
                {
                    TempData["ErrorMessage"] = "Mật khẩu mới phải khác mật khẩu cũ!";
                    return View();
                }

                int customerId = (int)Session["CustomerId"];
                var db = new CSDLDataContext();
                var customer = db.Khach_Hangs.FirstOrDefault(k => k.khach_hang_id == customerId);

                if (customer == null)
                {
                    return RedirectToAction("Logout");
                }

                // ✅ Kiểm tra mật khẩu hiện tại
                string currentPasswordHash = WebCinema.Services.AuthService.HashPassword(CurrentPassword);
                if (customer.mat_khau != currentPasswordHash)
                {
                    TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng!";
                    return View();
                }

                // ✅ Hash mật khẩu mới
                string newPasswordHash = WebCinema.Services.AuthService.HashPassword(NewPassword);

                // ✅ Cập nhật mật khẩu
                customer.mat_khau = newPasswordHash;
                db.SubmitChanges();

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
                
                // ✅ Redirect sang Login (logout)
                return RedirectToAction("Logout");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return View();
            }
        }

        // ✅ GET: Account/ForgotPassword
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // ✅ POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var db = new CSDLDataContext();
                var customer = db.Khach_Hangs.FirstOrDefault(k => k.email.ToLower() == model.Email.ToLower());

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Email không tồn tại trong hệ thống!";
                    return View(model);
                }

                // ✅ Tạo mật khẩu tạm thời (8 ký tự ngẫu nhiên)
                string tempPassword = GenerateRandomPassword(8);
                string tempPasswordHash = AuthService.HashPassword(tempPassword);

                // ✅ Cập nhật mật khẩu tạm thời vào database
                customer.mat_khau = tempPasswordHash;
                db.SubmitChanges();

                // ✅ Gửi email chứa mật khẩu tạm thời
                string emailContent = GetForgotPasswordEmailTemplate(customer.ho_ten, tempPassword);
                var emailService = new EmailServiceMailKit();
                
                bool emailSent = emailService.SendInvoiceEmail(
                    recipientEmail: customer.email,
                    recipientName: customer.ho_ten,
                    fileName: "",
                    filePath: "",
                    htmlContent: emailContent
                );

                if (emailSent)
                {
                    TempData["SuccessMessage"] = "Mật khẩu mới đã được gửi về email của bạn. Vui lòng kiểm tra hộp thư!";
                    return RedirectToAction("Login");
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau!";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return View(model);
            }
        }

        // ✅ Hàm tạo mật khẩu ngẫu nhiên
        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // ✅ Template email cho quên mật khẩu
        private string GetForgotPasswordEmailTemplate(string customerName, string newPassword)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ff6b35 0%, #ff8555 100%); color: white; padding: 30px 20px; text-align: center; border-radius: 12px 12px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border: 1px solid #e0e0e0; }}
        .footer {{ background: #333; color: white; padding: 20px; text-align: center; border-radius: 0 0 12px 12px; font-size: 12px; }}
        .info-box {{ background: white; padding: 20px; margin: 20px 0; border-left: 4px solid #ff6b35; border-radius: 8px; }}
        .info-box h3 {{ margin: 0 0 15px 0; color: #ff6b35; }}
        .info-box p {{ margin: 8px 0; }}
        .password-box {{ background: #fff3cd; border: 2px dashed #ffc107; padding: 20px; margin: 20px 0; text-align: center; border-radius: 8px; }}
        .password-text {{ font-size: 32px; font-weight: bold; color: #333; letter-spacing: 4px; font-family: 'Courier New', monospace; }}
        .warning-box {{ background: #f8d7da; border-left: 4px solid #dc3545; padding: 15px; margin: 20px 0; border-radius: 8px; }}
        .warning-box p {{ color: #721c24; margin: 5px 0; }}
        .divider {{ height: 1px; background: #e0e0e0; margin: 25px 0; }}
        a {{ color: #ff6b35; text-decoration: none; }}
        a:hover {{ text-decoration: underline; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎬 DAV CINEMA</h1>
            <p style='font-size: 18px; margin-top: 10px;'>Khôi phục mật khẩu</p>
        </div>

        <div class='content'>
            <p>Xin chào <strong>{customerName}</strong>,</p>
            
            <p>Chúng tôi đã nhận được yêu cầu khôi phục mật khẩu cho tài khoản của bạn tại <strong>DAV Cinema</strong>.</p>

            <div class='info-box'>
                <h3>🔑 Mật khẩu mới của bạn</h3>
                <p>Mật khẩu tạm thời đã được tạo thành công. Vui lòng sử dụng mật khẩu dưới đây để đăng nhập:</p>
            </div>

            <div class='password-box'>
                <p style='margin: 0 0 10px 0; font-size: 14px; color: #856404;'>MẬT KHẨU MỚI</p>
                <div class='password-text'>{newPassword}</div>
            </div>

            <div class='warning-box'>
                <p><strong>⚠️ LƯU Ý QUAN TRỌNG:</strong></p>
                <p>• Vui lòng <strong>đổi mật khẩu ngay</strong> sau khi đăng nhập thành công</p>
                <p>• Không chia sẻ mật khẩu này với bất kỳ ai</p>
                <p>• Nếu bạn không yêu cầu khôi phục mật khẩu, vui lòng liên hệ ngay với chúng tôi</p>
            </div>

            <div class='info-box'>
                <h3>📝 Hướng dẫn đăng nhập</h3>
                <p>1. Truy cập trang đăng nhập DAV Cinema</p>
                <p>2. Nhập email: <strong>{customerName}</strong></p>
                <p>3. Nhập mật khẩu mới ở trên</p>
                <p>4. Vào <strong>Tài khoản</strong> → <strong>Đổi mật khẩu</strong> để đổi mật khẩu mới</p>
            </div>

            <div class='divider'></div>

            <p><strong>Cần hỗ trợ?</strong></p>
            <p>
                📞 Hotline: 1900-1234-5678<br>
                📧 Email: support@davcinema.vn<br>
                🌐 Website: <a href='https://www.davcinema.vn'>www.davcinema.vn</a>
            </p>

            <p style='margin-top: 20px;'>Cảm ơn bạn đã tin tưởng DAV Cinema!</p>
        </div>

        <div class='footer'>
            <p>&copy; 2024 DAV Cinema. All rights reserved.</p>
            <p>Đây là email tự động, vui lòng không trả lời email này.</p>
            <p style='margin-top: 10px; color: #999;'>Email được gửi lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
        </div>
    </div>
</body>
</html>
";
        }
    }
}