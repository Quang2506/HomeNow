using System;
using System.Web.Mvc;
using System.Web.Security;
using HomeNow.ViewModels;
using Services.Interfaces;
using Services.Implementations;

namespace HomeNow.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController()
        {
            _userService = new UserService();   // dùng đúng UserService hiện tại
        }

       

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginPhoneViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginPhoneViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

                return View(model);
            }

        
            var user = _userService.ValidateLogin(model.LoginName, model.Password);
            if (user == null)
            {
                var msg = "Sai tài khoản hoặc mật khẩu.";

                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = msg });

                ModelState.AddModelError("", msg);
                return View(model);
            }

          
            FormsAuthentication.SetAuthCookie(user.Id.ToString(), model.RememberMe);

        
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

            if (Request.IsAjaxRequest())
                return Json(new { success = true });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

     

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

                return View(model);
            }

            try
            {
             
                var user = _userService.Register(
                    email: string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                    phoneNumber: model.PhoneNumber.Trim(),
                    password: model.Password
                );

                FormsAuthentication.SetAuthCookie(user.Id.ToString(), true);
                Session["CurrentUserName"] = user.DisplayName ?? user.PhoneNumber ?? "Tài khoản";

                if (Request.IsAjaxRequest())
                    return Json(new { success = true });

                return RedirectToAction("Index", "Home");
            }
            catch (InvalidOperationException ex)
            {
              
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = ex.Message });

                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

      

        [HttpGet]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session["CurrentUserName"] = null;
            return RedirectToAction("Index", "Home");
        }
    }
}
