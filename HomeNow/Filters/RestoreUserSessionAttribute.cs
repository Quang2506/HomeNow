using System;
using System.Web.Mvc;
using Core.Models;
using Services.Interfaces;
using Services.Implementations;

namespace HomeNow.Filters
{
   
    public class RestoreUserSessionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var ctx = filterContext.HttpContext;
            if (ctx == null || ctx.Session == null) return;

        
            if (!ctx.Request.IsAuthenticated) return;

         
            if (ctx.Session["CurrentUserId"] != null && ctx.Session["CurrentUserName"] != null)
                return;

         
            var idStr = ctx.User?.Identity?.Name;
            if (!int.TryParse(idStr, out var userId)) return;

          
            IUserService userService =
                DependencyResolver.Current.GetService<IUserService>() ?? new UserService();

            var user = userService.GetById(userId);
            if (user == null) return;

            ctx.Session["CurrentUserId"] = user.Id;
            ctx.Session["CurrentUserName"] = BuildDisplayName(user);
        }

        private static string BuildDisplayName(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.DisplayName))
                return user.DisplayName;

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var idx = user.Email.IndexOf("@", StringComparison.Ordinal);
                return idx > 0 ? user.Email.Substring(0, idx) : user.Email;
            }

            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                return user.PhoneNumber;

            return "Tài khoản";
        }
    }
}
