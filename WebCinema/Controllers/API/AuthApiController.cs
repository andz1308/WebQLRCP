using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using WebCinema.Infrastructure;
using WebCinema.Models;
using WebCinema.Services;
using System.Web;
using System.Web.Security;
using System.Security.Principal;

namespace WebCinema.Controllers.API
{
    /// <summary>
    /// Authentication API - Đăng nhập, đăng ký, cập nhật profile
    /// </summary>
    [RoutePrefix("api/auth")]
    public class AuthApiController : ApiController
    {
        private AuthService authService = new AuthService();
        private CSDLDataContext db = new CSDLDataContext();

        /// <summary>
        /// POST: api/auth/login
        /// Đăng nhập và nhận session
        /// </summary>
        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public IHttpActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                // ✅ ModelState Validation
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    LoggingHelper.LogError(new Exception($"ModelState invalid: {string.Join(", ", errors.Select(e => e.ErrorMessage))}"));
                    return BadRequest("Invalid request format: " + string.Join(", ", errors.Select(e => e.ErrorMessage)));
                }

                // ✅ Validation
                if (request == null)
                {
                    return BadRequest("Request body is required");
                }

                if (string.IsNullOrWhiteSpace(request.email) || string.IsNullOrWhiteSpace(request.password))
                {
                    return BadRequest("Email và password không được rỗng");
                }

                // ✅ Authenticate
                var auth = authService.Login(request.email, request.password);
                if (auth == null || !auth.IsAuthenticated)
                {
                    LoggingHelper.LogInfo($"Failed login: {request.email}");
                    return Ok(new { success = false, message = "Email hoặc mật khẩu không đúng" });
                }

                // ✅ Create session / set principal so [Authorize] works
                try
                {
                    var ctx = System.Web.HttpContext.Current;
                    if (ctx != null && ctx.Session != null)
                    {
                        // ✅ FIX: Kiểm tra Session null trước khi dùng
                        if (auth.Role == "Customer" && auth.Customer != null)
                        {
                            try
                            {
                                ctx.Session["CustomerId"] = auth.Customer.khach_hang_id;
                                ctx.Session["UserRole"] = "Customer";
                            }
                            catch (Exception sessionEx)
                            {
                                LoggingHelper.LogError(sessionEx, "Setting Customer session");
                            }
                        }
                        else if (auth.Role == "Staff" || auth.Role == "Admin")
                        {
                            if (auth.Employee != null)
                            {
                                try
                                {
                                    ctx.Session["EmployeeId"] = auth.Employee.nhanvien_id;
                                    ctx.Session["UserRole"] = auth.Role;
                                }
                                catch (Exception sessionEx)
                                {
                                    LoggingHelper.LogError(sessionEx, "Setting Employee session");
                                }
                            }
                        }
                    }

                    // Create FormsAuthenticationTicket that includes role in UserData
                    var ticket = new FormsAuthenticationTicket(
                        1,
                        request.email,
                        DateTime.Now,
                        DateTime.Now.AddHours(8),
                        false,
                        auth.Role ?? string.Empty
                    );

                    var encryptedTicket = FormsAuthentication.Encrypt(ticket);
                    var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                    {
                        HttpOnly = true,
                        Secure = ctx != null ? ctx.Request.IsSecureConnection : false
                    };

                    if (ctx != null)
                    {
                        ctx.Response.Cookies.Add(cookie);
                    }

                    // Set current principal for this request
                    string[] roles = new string[] { auth.Role ?? string.Empty };
                    var identity = new GenericIdentity(request.email);
                    var principal = new GenericPrincipal(identity, roles);

                    if (ctx != null)
                    {
                        ctx.User = principal;
                    }
                    System.Threading.Thread.CurrentPrincipal = principal;
                }
                catch (Exception ex)
                {
                    // don't fail login if session/cookie set fails, just log
                    LoggingHelper.LogError(ex, "Setting auth cookie/principal");
                }

                // ✅ Response
                object userData;
                if (auth.Role == "Customer" && auth.Customer != null)
                {
                    userData = new
                    {
                        user_id = auth.Customer.khach_hang_id,
                        name = auth.Customer.ho_ten,
                        email = auth.Customer.email,
                        phone = auth.Customer.so_dien_thoai,
                        role = "Customer"
                    };
                }
                else if (auth.Role == "Admin" || auth.Role == "Staff")
                {
                    if (auth.Employee == null)
                    {
                        LoggingHelper.LogError(new Exception($"Login: auth.Employee is null for role {auth.Role}"));
                        return Ok(new { success = false, message = "Employee data not found" });
                    }

                    userData = new
                    {
                        user_id = auth.Employee.nhanvien_id,
                        name = auth.Employee.ho_ten,
                        email = auth.Employee.email,
                        phone = auth.Employee.so_dien_thoai,
                        role = auth.Role
                    };
                }
                else
                {
                    LoggingHelper.LogError(new Exception($"Login: Unknown role {auth.Role}"));
                    return Ok(new { success = false, message = "Unknown user role" });
                }

                LoggingHelper.LogInfo($"Login success: {request.email} ({auth.Role})");
                return Ok(new
                {
                    success = true,
                    message = "Đăng nhập thành công",
                    user = userData
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// POST: api/auth/register
        /// Đăng ký khách hàng mới
        /// </summary>
        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        public IHttpActionResult Register([FromBody] RegisterRequest request)
        {
            try
            {
                // ✅ Validation
                if (request == null)
                {
                    return BadRequest("Request body is required");
                }

                if (string.IsNullOrWhiteSpace(request.email) || 
                    string.IsNullOrWhiteSpace(request.password) ||
                    string.IsNullOrWhiteSpace(request.name))
                {
                    return BadRequest("Các trường bắt buộc không được rỗng");
                }

                if (request.password.Length < 6)
                {
                    return BadRequest("Mật khẩu phải có ít nhất 6 ký tự");
                }

                // ✅ Check email already exists
                var existingCustomer = db.Khach_Hangs.FirstOrDefault(k => k.email == request.email);
                if (existingCustomer != null)
                {
                    return Ok(new { success = false, message = "Email đã được đăng ký" });
                }

                // ✅ Register - tạo trực tiếp
                var newCustomer = new Khach_Hang
                {
                    ho_ten = request.name,
                    email = request.email,
                    so_dien_thoai = request.phone,
                    mat_khau = AuthService.HashPassword(request.password),
                    ngay_dang_ky = DateTime.Now
                };

                db.Khach_Hangs.InsertOnSubmit(newCustomer);
                db.SubmitChanges();

                LoggingHelper.LogInfo($"New customer registered: {request.email}");
                return Ok(new
                {
                    success = true,
                    message = "Đăng ký thành công! Vui lòng đăng nhập"
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/auth/profile/{userId}
        /// Lấy thông tin cá nhân (yêu cầu xác thực)
        /// </summary>
        [HttpGet]
        [Route("profile/{userId}")]
        [Authorize]
        public IHttpActionResult GetProfile(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest("User ID không hợp lệ");
                }

                // ✅ Lấy thông tin khách hàng
                var customer = authService.GetCustomerById(userId);
                if (customer != null)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            user_id = customer.khach_hang_id,
                            name = customer.ho_ten,
                            email = customer.email,
                            phone = customer.so_dien_thoai,
                            role = "Customer"
                        }
                    });
                }

                // ✅ Lấy thông tin nhân viên
                var employee = authService.GetEmployeeById(userId);
                if (employee != null)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            user_id = employee.nhanvien_id,
                            name = employee.ho_ten,
                            email = employee.email,
                            phone = employee.so_dien_thoai,
                            role = "Staff"
                        }
                    });
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// PUT: api/auth/profile/{userId}
        /// Cập nhật thông tin cá nhân (yêu cầu xác thực)
        /// </summary>
        [HttpPut]
        [Route("profile/{userId}")]
        [Authorize]
        public IHttpActionResult UpdateProfile(int userId, [FromBody] UpdateProfileRequest request)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest("User ID không hợp lệ");
                }

                // ✅ Validation
                if (request == null)
                {
                    return BadRequest("Request body is required");
                }

                // ✅ Update khách hàng
                var customer = db.Khach_Hangs.FirstOrDefault(k => k.khach_hang_id == userId);
                if (customer != null)
                {
                    if (!string.IsNullOrWhiteSpace(request.name))
                        customer.ho_ten = request.name;

                    if (!string.IsNullOrWhiteSpace(request.phone))
                        customer.so_dien_thoai = request.phone;

                    db.SubmitChanges();

                    LoggingHelper.LogInfo($"Profile updated: {userId}");
                    return Ok(new
                    {
                        success = true,
                        message = "Cập nhật thành công"
                    });
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// POST: api/auth/change-password
        /// Đổi mật khẩu (yêu cầu xác thực)
        /// </summary>
        [HttpPost]
        [Route("change-password")]
        [Authorize]
        public IHttpActionResult ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // ✅ Validation
                if (request == null)
                {
                    return BadRequest("Request body is required");
                }

                if (string.IsNullOrWhiteSpace(request.current_password) || 
                    string.IsNullOrWhiteSpace(request.new_password))
                {
                    return BadRequest("Mật khẩu không được rỗng");
                }

                // ✅ Lấy user từ session - safe check
                if (System.Web.HttpContext.Current == null || 
                    System.Web.HttpContext.Current.Session == null)
                {
                    return Unauthorized();
                }

                var customerId = System.Web.HttpContext.Current.Session["CustomerId"] as int?;
                var employeeId = System.Web.HttpContext.Current.Session["EmployeeId"] as int?;

                if (!customerId.HasValue && !employeeId.HasValue)
                {
                    return Unauthorized();
                }

                // ✅ Update password cho khách hàng
                if (customerId.HasValue)
                {
                    var customer = db.Khach_Hangs.FirstOrDefault(k => k.khach_hang_id == customerId);
                    if (customer == null)
                        return NotFound();

                    // Verify current password
                    var auth = authService.Login(customer.email, request.current_password);
                    if (auth == null || !auth.IsAuthenticated)
                        return Ok(new { success = false, message = "Mật khẩu hiện tại không đúng" });

                    // Update password
                    customer.mat_khau = AuthService.HashPassword(request.new_password);
                    db.SubmitChanges();

                    LoggingHelper.LogInfo($"Password changed: {customer.email}");
                    return Ok(new
                    {
                        success = true,
                        message = "Đổi mật khẩu thành công"
                    });
                }

                // ✅ Update password cho nhân viên
                if (employeeId.HasValue)
                {
                    var employee = db.Nhan_Viens.FirstOrDefault(e => e.nhanvien_id == employeeId);
                    if (employee == null)
                        return NotFound();

                    // Verify current password
                    var auth = authService.Login(employee.email, request.current_password);
                    if (auth == null || !auth.IsAuthenticated)
                        return Ok(new { success = false, message = "Mật khẩu hiện tại không đúng" });

                    // Update password
                    employee.mat_khau = AuthService.HashPassword(request.new_password);
                    db.SubmitChanges();

                    LoggingHelper.LogInfo($"Password changed: {employee.email}");
                    return Ok(new
                    {
                        success = true,
                        message = "Đổi mật khẩu thành công"
                    });
                }

                return Unauthorized();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
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

    // ============================================================
    // REQUEST MODELS
    // ============================================================
    public class LoginRequest
    {
        public string email { get; set; }
        public string password { get; set; }
    }

    public class RegisterRequest
    {
        public string email { get; set; }
        public string password { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string name { get; set; }
        public string phone { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string current_password { get; set; }
        public string new_password { get; set; }
    }
}
