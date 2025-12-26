using System;
using System.Web;
using System.Web.Mvc;

namespace WebCinema.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        public string Roles { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
            }

            // Check if user is authenticated
            if (!httpContext.User.Identity.IsAuthenticated)
            {
                return false;
            }

            // Check session for role
            var userRole = httpContext.Session["UserRole"] as string;
            
            if (string.IsNullOrEmpty(Roles))
            {
                return true; // No specific role required
            }

            // Check if user has required role
            var requiredRoles = Roles.Split(',');
            foreach (var role in requiredRoles)
            {
                if (userRole != null && userRole.Trim().Equals(role.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                // Redirect to login if not authenticated
                filterContext.Result = new RedirectResult("~/Account/Login");
            }
            else
            {
                // Redirect to access denied if authenticated but not authorized
                filterContext.Result = new RedirectResult("~/Home/Index");
            }
        }
    }
}