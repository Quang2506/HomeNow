using Core.Models;
using Data;
using HomeNow.Services.Implementations;
using HomeNow.Services.Interfaces;
using HomeNow.ViewModels;
using Services.Implementations;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace HomeNow.Controllers
{
    public class PropertyController : Controller
    {
        private readonly IPropertyService _propertyService;
        private readonly IVrService _vrService;
        private readonly IFavoriteService _favoriteService;

        // Constructor mặc định (không dùng DI container)
        public PropertyController()
        {
            var db = new AppDbContext();
            _propertyService = new PropertyService(db);
            _vrService = new VrService(db);
            _favoriteService = new FavoriteService(db);
        }

        // Nếu bạn có DI container thì thêm constructor khác nhận vào IPropertyService, IVrService, IFavoriteService.

        // ================== MÀN DANH SÁCH NHÀ ==================

        // GET: /Property/List?lang=vi
        public async Task<ActionResult> List(string lang = "vi")
        {
            lang = (lang ?? "vi").ToLower();

            var all = await _propertyService.GetAllWithTranslationsAsync();

            // Nếu user đã đăng nhập -> lấy danh sách căn yêu thích
            HashSet<int> favoriteIds = null;
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.Identity.Name);
                var favList = _favoriteService.GetFavorites(userId); // List<int>
                favoriteIds = new HashSet<int>(favList);
            }

            var list = all.Select(p =>
            {
                var t = p.Translations?.FirstOrDefault(x => x.LangCode == lang);

                bool isFav = favoriteIds != null && favoriteIds.Contains(p.Id);

                return new PropertyListViewModel
                {
                    Id = p.Id,
                    Title = t?.DisplayTitle ?? p.DisplayTitle ?? p.Title,
                    Address = t?.Address ?? p.Address,
                    AreaName = t?.AreaName ?? p.AreaName,
                    CommunityName = t?.CommunityName ?? p.CommunityName,
                    RoomType = t?.RoomType ?? p.RoomType,
                    Orientation = t?.Orientation ?? p.Orientation,
                    District = t?.District ?? p.District,

                    CoverImageUrl = p.CoverImageUrl,
                    Price = p.PricePerMonth,
                    PriceUnit = p.PriceUnit,
                    AreaM2 = p.AreaM2,
                    IsVrAvailable = p.IsVrAvailable,

                    IsFavorite = isFav
                };
            }).ToList();

            ViewBag.Lang = lang;
            return View(list);
        }

        // ================== MÀN DANH SÁCH NHÀ YÊU THÍCH ==================

        // GET: /Property/Favorites?lang=vi
        [Authorize]
        public async Task<ActionResult> Favorites(string lang = "vi")
        {
            lang = (lang ?? "vi").ToLower();
            var userId = int.Parse(User.Identity.Name);

            var favoriteIds = _favoriteService.GetFavorites(userId); // List<int>
            if (favoriteIds == null || !favoriteIds.Any())
            {
                ViewBag.Lang = lang;
                return View(new List<PropertyListViewModel>());
            }

            var all = await _propertyService.GetAllWithTranslationsAsync();
            var favProps = all.Where(p => favoriteIds.Contains(p.Id));

            var list = favProps.Select(p =>
            {
                var t = p.Translations?.FirstOrDefault(x => x.LangCode == lang);

                return new PropertyListViewModel
                {
                    Id = p.Id,
                    Title = t?.DisplayTitle ?? p.DisplayTitle ?? p.Title,
                    Address = t?.Address ?? p.Address,
                    AreaName = t?.AreaName ?? p.AreaName,
                    CommunityName = t?.CommunityName ?? p.CommunityName,
                    RoomType = t?.RoomType ?? p.RoomType,
                    Orientation = t?.Orientation ?? p.Orientation,
                    District = t?.District ?? p.District,

                    CoverImageUrl = p.CoverImageUrl,
                    Price = p.PricePerMonth,
                    PriceUnit = p.PriceUnit,
                    AreaM2 = p.AreaM2,
                    IsVrAvailable = p.IsVrAvailable,

                    IsFavorite = true
                };
            }).ToList();

            ViewBag.Lang = lang;
            return View("List", list); // có thể dùng chung View List
        }

        // ================== BẬT/TẮT YÊU THÍCH ==================

        // POST: /Property/ToggleFavorite/5
        [Authorize]
        [HttpPost]
        public ActionResult ToggleFavorite(int id, string returnUrl, string lang = "vi")
        {
            var userId = int.Parse(User.Identity.Name);

            _favoriteService.Toggle(userId, id);

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("List", new { lang });
        }

        // ================== MÀN VR CHO 1 NHÀ ==================

        // GET: /Property/Vr/1?lang=vi
        public async Task<ActionResult> Vr(int id, string lang = "vi")
        {
            lang = (lang ?? "vi").ToLower();

            var scenes = await _vrService.GetScenesForPropertyAsync(id);
            if (scenes == null || !scenes.Any())
            {
                return Content("Không có dữ liệu VR cho căn nhà này.");
            }

            var vm = new VrViewModel
            {
                PropertyId = id,
                Lang = lang,
                Scenes = scenes
            };

            return View(vm);
        }
    }
}
