using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

using Core.Models;        // HomeIndexViewModel, PropertyListItemViewModel, dropdown items
using Core.ViewModels;    // PropertyListViewModel (service trả về)
using Data;               // AppDbContext

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

        private int? GetCurrentUserId()
        {
            if (Session["CurrentUserId"] is int id1) return id1;

            if (Session["CurrentUserId"] != null &&
                int.TryParse(Session["CurrentUserId"].ToString(), out var parsed))
                return parsed;

            if (Session["UserId"] is int id2) return id2;

            if (Session["UserId"] != null &&
                int.TryParse(Session["UserId"].ToString(), out var parsed2))
                return parsed2;

            return null;
        }

        private string GetLang2()
        {
            var uiLang = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            return uiLang.Length >= 2 ? uiLang.Substring(0, 2).ToLower() : "vi";
        }

        private string NormalizeMode(string mode)
        {
            var m = (mode ?? "rent").Trim().ToLower();
            return (m == "sale") ? "sale" : "rent";
        }

        // ================== FAVORITE ==================

        [HttpPost]
        public async Task<ActionResult> ToggleFavorite(int propertyId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Json(new { success = false, needLogin = true }, JsonRequestBehavior.DenyGet);

            var isFav = await _favoriteService.ToggleFavoriteAsync(userId.Value, propertyId);

            // trả count để badge update ngay
            var lang = GetLang2();
            var favList = await _favoriteService.GetFavoritesAsync(userId.Value, lang);
            var favoriteCount = favList?.Count ?? 0;

            return Json(new
            {
                success = true,
                isFavorite = isFav,
                favoriteCount = favoriteCount
            }, JsonRequestBehavior.DenyGet);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> Favorites()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return RedirectToAction("Index", "Home");

            var lang = GetLang2();
            var list = await _favoriteService.GetFavoritesAsync(userId.Value, lang);
            return View(list);
        }

        // Realtime summary: load trang / vừa login xong gọi để sync badge + tim is-fav
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> FavoriteSummary()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Json(new { isAuth = false, favoriteCount = 0, favoriteIds = new int[0] }, JsonRequestBehavior.AllowGet);

            var lang = GetLang2();
            var favList = await _favoriteService.GetFavoritesAsync(userId.Value, lang);

            var ids = (favList ?? new List<PropertyListItemViewModel>())
                        .Select(x => x.PropertyId)
                        .Distinct()
                        .ToArray();

            return Json(new { isAuth = true, favoriteCount = ids.Length, favoriteIds = ids }, JsonRequestBehavior.AllowGet);
        }

        // Giữ endpoint cũ để các view đang gọi vẫn chạy
        [HttpGet]
        [AllowAnonymous]
        public Task<ActionResult> HeaderFavoriteInfo()
        {
            return FavoriteSummary();
        }

        // ================== LIST (FLOW GIỐNG HOME) ==================

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> List(
            string mode = "rent",
            int? cityId = null,
            string priceRange = null,
            string propertyType = null,
            string keyword = null,
            int page = 1,
            int pageSize = 16)
        {
            mode = NormalizeMode(mode);
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 16;

            var lang = GetLang2();

           
            var vm = new HomeIndexViewModel
            {
                TransactionType = mode,
                CityId = cityId,
                PriceRange = priceRange,
                PropertyType = propertyType,
                Keyword = keyword,

                HasSearch = true,     // List luôn là dạng search/list
                Page = page,
                PageSize = pageSize
            };

            // dropdown + background giống Home
            FillDropDownAndHero(vm, lang);

            // favorites set + badge initial
            var userId = GetCurrentUserId();
            HashSet<int> favoriteIds = null;
            if (userId.HasValue)
            {
                var favList = await _favoriteService.GetFavoritesAsync(userId.Value, lang);
                favoriteIds = new HashSet<int>((favList ?? new List<PropertyListItemViewModel>()).Select(f => f.PropertyId));
            }
            ViewBag.FavoriteCount = favoriteIds?.Count ?? 0;

            // lấy dữ liệu list giống Home search
            var all = await _propertyService.SearchAsync(lang, mode, cityId, priceRange, propertyType, keyword);
            var total = all?.Count ?? 0;

            vm.TotalItems = total;
            vm.TotalPages = (int)Math.Ceiling(total / (double)vm.PageSize);
            if (vm.TotalPages <= 0) vm.TotalPages = 1;
            if (vm.Page > vm.TotalPages) vm.Page = vm.TotalPages;

            var pageData = (all ?? new List<PropertyListViewModel>())
                .Skip((vm.Page - 1) * vm.PageSize)
                .Take(vm.PageSize)
                .ToList();

            vm.SearchResults = pageData.Select(x => ToHomeItem(x, favoriteIds)).ToList();

            return View(vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> LoadMoreList(
            string mode = "rent",
            int? cityId = null,
            string priceRange = null,
            string propertyType = null,
            string keyword = null,
            int page = 2,
            int pageSize = 16)
        {
            mode = NormalizeMode(mode);
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 16;

            var lang = GetLang2();

            var userId = GetCurrentUserId();
            HashSet<int> favoriteIds = null;
            if (userId.HasValue)
            {
                var favList = await _favoriteService.GetFavoritesAsync(userId.Value, lang);
                favoriteIds = new HashSet<int>((favList ?? new List<PropertyListItemViewModel>()).Select(f => f.PropertyId));
            }

            var all = await _propertyService.SearchAsync(lang, mode, cityId, priceRange, propertyType, keyword);
            var total = all?.Count ?? 0;
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages <= 0) totalPages = 1;

            if (page > totalPages) return Content("");

            var pageData = (all ?? new List<PropertyListViewModel>())
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var list = pageData.Select(x => ToHomeItem(x, favoriteIds)).ToList();

            return PartialView("~/Views/Home/_PropertyRowList.cshtml", list);

            // Nếu partial của bạn nằm ở /Views/Property/_PropertyRowList.cshtml thì đổi dòng trên thành:
            // return PartialView("~/Views/Property/_PropertyRowList.cshtml", list);
        }

        // ================== helpers ==================

        private void FillDropDownAndHero(HomeIndexViewModel vm, string lang)
        {
            using (var db = new AppDbContext())
            {
                var cities = db.Cities.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList();
                vm.Cities = cities.Select(c => new CityDropDownItem
                {
                    CityId = c.CityId,
                    Name = lang == "en" ? (c.NameEn ?? c.NameVi)
                         : lang == "zh" ? (c.NameZh ?? c.NameVi)
                         : c.NameVi,
                    BackgroundUrl = c.BackgroundUrl
                }).ToList();

                var priceFilters = db.PriceFilters.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).ToList();
                vm.PriceFilters = priceFilters.Select(p => new PriceFilterDropDownItem
                {
                    Code = p.Code,
                    Name = lang == "en" ? (p.NameEn ?? p.NameVi)
                         : lang == "zh" ? (p.NameZh ?? p.NameVi)
                         : p.NameVi,
                    MinPrice = p.MinPrice,
                    MaxPrice = p.MaxPrice
                }).ToList();

                var propTypes = db.PropertyTypes.Where(t => t.IsActive).OrderBy(t => t.DisplayOrder).ToList();
                vm.PropertyTypes = propTypes.Select(t => new PropertyTypeDropDownItem
                {
                    Code = t.Code,
                    Name = lang == "en" ? (t.NameEn ?? t.NameVi)
                         : lang == "zh" ? (t.NameZh ?? t.NameVi)
                         : t.NameVi
                }).ToList();

                var cityForBg = vm.CityId.HasValue
                    ? cities.FirstOrDefault(c => c.CityId == vm.CityId.Value)
                    : cities.FirstOrDefault();

                ViewBag.HeroBackground = cityForBg?.BackgroundUrl ?? "/Assets/Banner.jpg";
            }
        }

        private PropertyListItemViewModel ToHomeItem(PropertyListViewModel x, ISet<int> favoriteIds)
        {
            var listingType = string.IsNullOrWhiteSpace(x.ListingType) ? null : x.ListingType;

            var price = x.Price ?? 0m;
            var priceLabel = price > 0m ? FormatPriceLabel(price, listingType) : "";

            var area = x.AreaSqm.HasValue ? (float)x.AreaSqm.Value : (float)(x.AreaM2 ?? 0m);

            return new PropertyListItemViewModel
            {
                PropertyId = x.Id,
                Title = x.Title,
                Address = x.Address,

                Price = price,
                PriceLabel = priceLabel,

                Area = area,
                Bed = x.BedroomCount,
                Bath = x.BathroomCount,

                ThumbnailUrl = x.CoverImageUrl,

                ListingType = x.ListingType,
                PropertyType = x.PropertyType,

                IsFavorite = favoriteIds != null && favoriteIds.Contains(x.Id)
            };
        }

        private string FormatPriceLabel(decimal price, string listingType)
        {
            var vi = CultureInfo.GetCultureInfo("vi-VN");
            var s = price.ToString("#,0", vi);

            if ((listingType ?? "").ToLower() == "rent")
                s += " ";

            return s;
        }
    }
}
