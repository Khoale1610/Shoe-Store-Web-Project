using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace QL_ShopGiay.Helpers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var adminId = session.GetInt32("AdminId");
            var role = session.GetString("Role");

            // Bảo mật: Chỉ cho phép tài khoản Quản trị viên có role = 'admin' truy cập
            if (!adminId.HasValue || !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                var factory = context.HttpContext.RequestServices.GetService<ITempDataDictionaryFactory>();
                if (factory != null)
                {
                    var tempData = factory.GetTempData(context.HttpContext);
                    tempData["ToastWarning"] = "Khu vực hạn chế! Chỉ tài khoản Quản trị viên (role = 'admin') mới có quyền truy cập.";
                }

                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
