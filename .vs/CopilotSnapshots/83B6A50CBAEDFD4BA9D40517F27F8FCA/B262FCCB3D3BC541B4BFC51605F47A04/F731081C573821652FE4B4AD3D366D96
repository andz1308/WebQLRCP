using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Http;  // ✅ THÊM
using WebCinema.Services;
using System.Web.Security;
using System.Security.Principal;

namespace WebCinema
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            
            // ✅ QUAN TRỌNG: Web API config phải gọi TRƯỚC MVC routes!
            GlobalConfiguration.Configure(WebApiConfig.Register);
            
            // Sau đó mới gọi MVC routes
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // ✅ BẮT ĐẦU DỊCH VỤ HỦY ĐƠN QUÁ HẠN
            // TODO: Uncomment after ensuring BookingExpirationService compiles
            try
            {
                BookingExpirationService.StartExpirationCheck();
            }
            catch (Exception ex)
            {
                // Log startup error but don't crash app
                System.Diagnostics.Debug.WriteLine($"Error starting BookingExpirationService: {ex.Message}");
            }
        }

        // Restore session values from authenticated identity when Session becomes available
        protected void Application_AcquireRequestState(object sender, EventArgs e)
        {
            try
            {
                var context = HttpContext.Current;
                if (context == null) return;

                // Ensure session state is available
                var session = context.Session;
                if (session == null) return;

                // If user is authenticated but session is empty, try to rehydrate from identity
                if (context.User != null && context.User.Identity != null && context.User.Identity.IsAuthenticated)
                {
                    var email = context.User.Identity.Name;
                    if (string.IsNullOrEmpty(email)) return;

                    // ✅ FIX: Kiểm tra cả Customer và Employee
                    // Nếu Session rỗng, restore lại
                    if (session["CustomerId"] == null && session["EmployeeId"] == null)
                    {
                        try
                        {
                            var authService = new Services.AuthService();
                            
                            // Thử tìm Customer trước
                            var customer = authService.GetCustomerByEmail(email);
                            if (customer != null)
                            {
                                session["CustomerId"] = customer.khach_hang_id;
                                session["CustomerName"] = customer.ho_ten;
                                session["CustomerEmail"] = customer.email;
                                session["UserRole"] = "Customer";
                                return;
                            }
                            
                            // ✅ Nếu không phải Customer, thử tìm Employee
                            var db = new Models.CSDLDataContext();
                            var employee = db.Nhan_Viens.FirstOrDefault(emp => emp.email == email);
                            if (employee != null)
                            {
                                session["EmployeeId"] = employee.nhanvien_id;
                                session["EmployeeName"] = employee.ho_ten;
                                session["EmployeeEmail"] = employee.email;
                                
                                // Xác định role
                                string role = "Staff";
                                if (employee.role_id.HasValue)
                                {
                                    var roleObj = db.Roles.FirstOrDefault(r => r.role_id == employee.role_id.Value);
                                    if (roleObj != null && roleObj.ten_role != null)
                                    {
                                        var roleName = roleObj.ten_role.Trim();
                                        if (roleName.Equals("admin", StringComparison.OrdinalIgnoreCase) || 
                                            roleName.IndexOf("admin", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            role = "Admin";
                                        }
                                    }
                                }
                                
                                session["UserRole"] = role;
                            }
                        }
                        catch
                        {
                            // ignore errors while restoring session
                        }
                    }
                }
            }
            catch
            {
                // swallow exceptions to avoid breaking pipeline
            }
        }

        // Set principal from forms auth cookie for Web API [Authorize(Roles=...)] checks
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            try
            {
                var ctx = HttpContext.Current;
                if (ctx == null) return;

                var authCookie = ctx.Request.Cookies[FormsAuthentication.FormsCookieName];
                if (authCookie == null || string.IsNullOrEmpty(authCookie.Value)) return;

                var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                if (ticket == null) return;

                var role = ticket.UserData ?? string.Empty;
                var identity = new GenericIdentity(ticket.Name, "Forms");
                string[] roles = string.IsNullOrEmpty(role) ? new string[0] : new[] { role };

                var principal = new GenericPrincipal(identity, roles);
                ctx.User = principal;
                System.Threading.Thread.CurrentPrincipal = principal;
            }
            catch
            {
                // ignore failures
            }
        }

        // ✅ DỪNG DỊCH VỤ KHI APP TẮT
        protected void Application_End()
        {
            BookingExpirationService.StopExpirationCheck();
        }
    }
}
