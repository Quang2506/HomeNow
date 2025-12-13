using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Services.Interfaces;
using Services.Implementations;

namespace HomeNow.Controllers
{
    public class FavoriteController : Controller
    {
        private readonly IFavoriteService _favoriteService;

        public FavoriteController()
        {
            _favoriteService = new FavoriteService();
        }

        private int? GetCurrentUserId()
        {
            var obj = Session["CurrentUserId"];
            if (obj == null) return null;

            // phòng trường hợp Session lưu string
            int id;
            return int.TryParse(obj.ToString(), out id) ? (int?)id : null;
        }

        // GET /Favorite
        public async Task<ActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (!User.Identity.IsAuthenticated || !userId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { returnUrl = Url.Action("Index", "Favorite") });
            }

            var langCode = System.Threading.Thread.CurrentThread
                .CurrentUICulture.Name.Substring(0, 2).ToLower();

            var list = await _favoriteService.GetFavoritesAsync(userId.Value, langCode);

            // View Favorites.cshtml: @model List<Core.ViewModels.PropertyListViewModel>
            return View(list);
        }

        // POST /Favorite/Toggle
        [HttpPost]
        public async Task<ActionResult> Toggle(int propertyId)
        {
            var userId = GetCurrentUserId();
            if (!User.Identity.IsAuthenticated || !userId.HasValue)
            {
                return Json(new { success = false, requiresLogin = true });
            }

            var isFav = await _favoriteService.ToggleFavoriteAsync(userId.Value, propertyId);

            return Json(new
            {
                success = true,
                isFavorite = isFav
            });
        }
    }
}
