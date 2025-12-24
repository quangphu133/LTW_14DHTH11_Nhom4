using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class BaseAdminController : Controller
    {
        // Hàm này chạy trước mọi Action trong Controller kế thừa nó
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var sessionRole = Session["Role"];

            // Nếu chưa đăng nhập hoặc không phải là Nhân viên
            if (sessionRole == null || sessionRole.ToString() != "NhanVien")
            {
                // Đá về trang đăng nhập
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(new { controller = "Users", action = "Login", area = "" })
                );
            }

            base.OnActionExecuting(filterContext);
        }
    }
}