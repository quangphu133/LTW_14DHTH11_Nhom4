using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebApplication1.Filters
{
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly string[] allowedRoles;

        public RoleAuthorizeAttribute(params string[] roles)
        {
            this.allowedRoles = roles;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            // 1. Lấy Session
            var userRole = filterContext.HttpContext.Session["ChucVu"] as string;
            var userName = filterContext.HttpContext.Session["SqlUser"] as string;

            // 2. DEBUG: Kiểm tra xem Session có bị Null không
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userRole))
            {
                filterContext.Controller.TempData["Loi"] = "LỖI SESSION: Phiên đăng nhập bị mất (Null). Vui lòng kiểm tra Web.config.";

                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "NHANVIENs", action = "Login" })
                );
                return;
            }

            // 3. DEBUG: Kiểm tra xem Quyền có khớp không (So sánh chuỗi)
            // Trim() để cắt khoảng trắng thừa nếu có
            if (!allowedRoles.Contains(userRole.Trim()))
            {
                string yeuCau = string.Join(", ", allowedRoles);
                filterContext.Controller.TempData["Loi"] = $"LỖI QUYỀN: Bạn đang có quyền ['{userRole}'] nhưng trang này yêu cầu ['{yeuCau}']. (Hãy kiểm tra chính tả/Unicode)";

                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "NHANVIENs", action = "Login" })
                );
                return;
            }
        }
    }
}