using System;
using System.Web;

namespace WebCinema.Infrastructure
{
    public static class AuthSessionHelper
    {
        public static void EnsureCustomerSession(HttpContextBase httpContext)
        {
            try
            {
                if (httpContext == null) return;
                var user = httpContext.User;
                var session = httpContext.Session;
                if (user != null && user.Identity != null && user.Identity.IsAuthenticated && session != null)
                {
                    if (session["CustomerId"] == null)
                    {
                        var email = user.Identity.Name;
                        if (!string.IsNullOrEmpty(email))
                        {
                            var authService = new WebCinema.Services.AuthService();
                            var customer = authService.GetCustomerByEmail(email);
                            if (customer != null)
                            {
                                session["CustomerId"] = customer.khach_hang_id;
                                session["CustomerName"] = customer.ho_ten;
                                session["CustomerEmail"] = customer.email;
                                session["UserRole"] = "Customer";
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore failures
            }
        }
    }
}
