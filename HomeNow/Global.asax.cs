using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace HomeNow
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        // 👇 THÊM HÀM NÀY
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // Mặc định: tiếng Việt
            var cultureName = "vi-VN";

            // Đọc cookie lang nếu có
            var cookie = HttpContext.Current.Request.Cookies["lang"];
            if (cookie != null && !string.IsNullOrEmpty(cookie.Value))
            {
                cultureName = cookie.Value;   // vi-VN / en-US / zh-CN
            }

            var culture = CultureInfo.CreateSpecificCulture(cultureName);

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}
