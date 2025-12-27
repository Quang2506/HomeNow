using Core.Models;
using Core.Resources;
using Services.Implementations;
using Services.Interfaces;
using System;
using System.Web.Mvc;
using System.Web.Security;

namespace HomeNow.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController()
        {
            _userService = new UserService();
        }

        // Login (AJAX)
        [HttpPost]
        public JsonResult LoginAjax(string loginName, string password)
        {
            var user = _userService.ValidateLogin(loginName, password);
            if (user == null)
                return Json(new { success = false, message = AuthTexts.Msg_InvalidCredentials });

            // Nếu có email mà chưa verify => bắt verify
            if (!string.IsNullOrWhiteSpace(user.Email) && (!user.EmailVerified || string.Equals(user.Status, "pending", StringComparison.OrdinalIgnoreCase)))
            {
                string err;
                _userService.SendVerifyEmailOtp(user.Email, out err);

                return Json(new
                {
                    success = false,
                    needVerify = true,
                    email = user.Email,
                    message = string.IsNullOrWhiteSpace(err) ? AuthTexts.Msg_NeedVerify : err
                });
            }

            SignIn(user);
            return Json(new { success = true });
        }

        // Register (AJAX)
        [HttpPost]
        public JsonResult RegisterAjax(string email, string phoneNumber, string displayName, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = AuthTexts.Msg_EmailRequired });

            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword) || password != confirmPassword)
                return Json(new { success = false, message = AuthTexts.Msg_PasswordNotMatch });

            if (_userService.IsEmailExists(email))
                return Json(new { success = false, message = AuthTexts.Msg_EmailExists });

            if (!string.IsNullOrWhiteSpace(phoneNumber) && _userService.IsPhoneExists(phoneNumber))
                return Json(new { success = false, message = AuthTexts.Msg_PhoneExists });

            var user = _userService.CreateUser(email, phoneNumber, displayName, password);

            // gửi OTP verify
            string err;
            _userService.SendVerifyEmailOtp(user.Email, out err);

            return Json(new
            {
                success = true,
                needVerify = true,
                email = user.Email,
                message = string.IsNullOrWhiteSpace(err) ? AuthTexts.Msg_OtpSent : err
            });
        }

        // Verify OTP (AJAX)
        [HttpPost]
        public JsonResult VerifyEmailOtpAjax(string email, string code)
        {
            User user;
            string err;

            var ok = _userService.VerifyEmailOtp(email, code, out user, out err);
            if (!ok)
                return Json(new { success = false, message = err ?? AuthTexts.Msg_OtpInvalid });

            // verify xong thì login luôn
            SignIn(user);
            return Json(new { success = true, message = AuthTexts.Msg_VerifySuccess });
        }

        [HttpPost]
        public JsonResult ResendVerifyOtpAjax(string email)
        {
            string err;
            var ok = _userService.ResendVerifyEmailOtp(email, out err);
            return Json(new
            {
                success = ok,
                message = ok ? AuthTexts.Msg_OtpResent : (err ?? AuthTexts.Msg_OtpSendFail)
            });
        }

        // Forgot password (AJAX)
        [HttpPost]
        public JsonResult SendResetOtpAjax(string email)
        {
            string err;
            var ok = _userService.SendResetPasswordOtp(email, out err);
            return Json(new
            {
                success = ok,
                message = ok ? AuthTexts.Msg_OtpSent : (err ?? AuthTexts.Msg_OtpSendFail)
            });
        }

        [HttpPost]
        public JsonResult ResetPasswordByOtpAjax(string email, string code, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword) || newPassword != confirmPassword)
                return Json(new { success = false, message = AuthTexts.Msg_PasswordNotMatch });

            string err;
            var ok = _userService.ResetPasswordByOtp(email, code, newPassword, out err);
            return Json(new
            {
                success = ok,
                message = ok ? AuthTexts.Msg_ResetSuccess : (err ?? AuthTexts.Msg_OtpInvalid)
            });
        }

      
        // Change password (AJAX) 
       
        [HttpPost]
        public JsonResult ChangePasswordAjax(string oldPassword, string newPassword, string confirmPassword)
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { success = false, needLogin = true, message = "Need login" });

            if (string.IsNullOrWhiteSpace(oldPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin." });

            if (newPassword != confirmPassword)
                return Json(new { success = false, message = AuthTexts.Msg_PasswordNotMatch });

            var uidObj = Session["CurrentUserId"];
            int userId = 0;
            if (uidObj != null) int.TryParse(uidObj.ToString(), out userId);

            if (userId <= 0)
                return Json(new { success = false, message = "Invalid session." });

            string err;
            var ok = _userService.ChangePassword(userId, oldPassword, newPassword, out err);

            return Json(new
            {
                success = ok,
                message = ok ? "Đổi mật khẩu thành công." : (err ?? "Đổi mật khẩu thất bại.")
            });
        }
        [HttpGet]
        public JsonResult GetProfileAjax()
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { success = false, needLogin = true }, JsonRequestBehavior.AllowGet);

            var uidObj = Session["CurrentUserId"];
            int userId = 0;
            if (uidObj != null) int.TryParse(uidObj.ToString(), out userId);

            if (userId <= 0)
                return Json(new { success = false, message = "Invalid session." }, JsonRequestBehavior.AllowGet);

            var user = _userService.GetById(userId);
            if (user == null)
                return Json(new { success = false, message = "User not found." }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,
                displayName = user.DisplayName ?? "",
                phoneNumber = user.PhoneNumber ?? "",
                email = user.Email ?? ""
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateProfileAjax(string displayName, string phoneNumber)
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { success = false, needLogin = true, message = "Need login" });

            var uidObj = Session["CurrentUserId"];
            int userId = 0;
            if (uidObj != null) int.TryParse(uidObj.ToString(), out userId);

            if (userId <= 0)
                return Json(new { success = false, message = "Invalid session." });

            string err;
            var ok = _userService.UpdateProfile(userId, displayName, phoneNumber, out err);

            if (ok)
            {
                // ✅ update header name realtime (giữ logic cũ: Session["CurrentUserName"])
                Session["CurrentUserName"] = string.IsNullOrWhiteSpace(displayName) ? AuthTexts.Account_Default : displayName.Trim();
            }

            return Json(new
            {
                success = ok,
                message = ok ? "Lưu thay đổi thành công." : (err ?? "Lưu thay đổi thất bại.")
            });
        }


        // Normal actions
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        private void SignIn(User user)
        {
            FormsAuthentication.SetAuthCookie(user.Email ?? user.PhoneNumber ?? user.Id.ToString(), false);
            Session["CurrentUserId"] = user.Id;
            Session["CurrentUserName"] = string.IsNullOrWhiteSpace(user.DisplayName) ? AuthTexts.Account_Default : user.DisplayName;

            // update last_login (optional)
        }
    }
}
