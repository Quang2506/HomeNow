using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

using Core.Models;
using Core.ViewModels;

using Services.Interfaces;
using Services.Implementations;


namespace HomeNow.Controllers
{
    public class PropertyController : Controller
    {
        private readonly IPropertyService _propertyService;
        private readonly IFavoriteService _favoriteService;

        public PropertyController()
        {
            _propertyService = new PropertyService();
            _favoriteService = new FavoriteService();
        }

        // Helper lấy userId từ Session
        private int? GetCurrentUserId()
        {
            if (Session["CurrentUserId"] is int id1)
                return id1;

            if (Session["CurrentUserId"] != null)
            {
                int parsed;
                if (int.TryParse(Session["CurrentUserId"].ToString(), out parsed))
                    return parsed;
            }

            if (Session["UserId"] is int id2)
                return id2;

            if (Session["UserId"] != null)
            {
                int parsed2;
                if (int.TryParse(Session["UserId"].ToString(), out parsed2))
                    return parsed2;
            }

            return null;
        }

        // ========== FAVORITE ==========

        [HttpPost]
        public async Task<ActionResult> ToggleFavorite(int propertyId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Json(new
                {
                    success = false,
                    needLogin = true
                }, JsonRequestBehavior.DenyGet);
            }

            var isFav = await _favoriteService.ToggleFavoriteAsync(userId.Value, propertyId);

            return Json(new
            {
                success = true,
                isFavorite = isFav
            }, JsonRequestBehavior.DenyGet);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> Favorites()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var uiLang = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            var lang = uiLang.Length >= 2 ? uiLang.Substring(0, 2).ToLower() : "vi";

            var list = await _favoriteService.GetFavoritesAsync(userId.Value, lang);

            return View(list);
        }

        // /Property/List?mode=rent&cityId=...&priceRange=...&propertyType=...&keyword=...
        public async Task<ActionResult> List(
            string mode = "rent",
            int? cityId = null,
            string priceRange = null,
            string propertyType = null,
            string keyword = null)
        {
            var uiLang = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            var langCode = uiLang.Length >= 2
                ? uiLang.Substring(0, 2).ToLower()
                : "vi";

            var list = await _propertyService.SearchAsync(
                langCode,
                mode,
                cityId,
                priceRange,
                propertyType,
                keyword);   // <-- THAM SỐ THỨ 6

            return View(list);
        }


       


    }
}
