using System;
using System.Security.Principal;
using System.Web.Mvc;
using System.Web.Security;
using Services.Interfaces;
using Services.Implementations;

namespace HomeNow.Controllers
{
    public abstract class BaseController : Controller
    {
        private readonly IUserService _userService = new UserService();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try { BootstrapAuthSession(); } catch { /* ignore */ }
            base.OnActionExecuting(filterContext);
        }

        private void BootstrapAuthSession()
        {
            if (!User.Identity.IsAuthenticated) return;

            // đã có session thì thôi
            if (Session["CurrentUserId"] != null && Session["CurrentUserName"] != null) return;

            var cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null || string.IsNullOrWhiteSpace(cookie.Value))
            {
                ForceSignOut();
                return;
            }

            FormsAuthenticationTicket ticket;
            try { ticket = FormsAuthentication.Decrypt(cookie.Value); }
            catch { ticket = null; }

            if (ticket == null)
            {
                ForceSignOut();
                return;
            }

       
            if (!int.TryParse(ticket.Name, out var userId))
            {
                ForceSignOut();
                return;
            }

            
            if (!ticket.IsPersistent)
            {
                ForceSignOut();
                return;
            }

        
            var user = _userService.GetById(userId);
            if (user == null)
            {
                ForceSignOut();
                return;
            }

            Session["CurrentUserId"] = user.Id;

            var displayName = user.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    var idx = user.Email.IndexOf("@", StringComparison.Ordinal);
                    displayName = idx > 0 ? user.Email.Substring(0, idx) : user.Email;
                }
                else if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    displayName = user.PhoneNumber;
                }
                else
                {
                    displayName = "Tài khoản";
                }
            }

            Session["CurrentUserName"] = displayName;
        }

        private void ForceSignOut()
        {
            FormsAuthentication.SignOut();
            Session["CurrentUserId"] = null;
            Session["CurrentUserName"] = null;

          
            HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
            System.Threading.Thread.CurrentPrincipal = HttpContext.User;
        }
    }
}
