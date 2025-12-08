using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HomeNow.Controllers
{
    public class LanguageController : Controller
    {
        public ActionResult Set(string lang, string returnUrl)
        {
            var supported = new[] { "vi-VN", "en-US", "zh-CN" };
            if (!supported.Contains(lang))
            {
                lang = "vi-VN";
            }

            var cookie = new HttpCookie("lang", lang)
            {
                Expires = DateTime.Now.AddYears(1)
            };
            Response.Cookies.Add(cookie);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
