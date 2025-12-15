using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

using Core.Models;
using Core.ViewModels;

using Data;

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

        // ========== FAVORITE ==========

        [HttpPost]
        public async Task<ActionResult> ToggleFavorite(int propertyId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Json(new { success = false, needLogin = true }, JsonRequestBehavior.DenyGet);

            var isFav = await _favoriteService.ToggleFavoriteAsync(userId.Value, propertyId);
            return Json(new { success = true, isFavorite = isFav }, JsonRequestBehavior.DenyGet);
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

        // ================= LIST =================

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

            var raw = await _propertyService.SearchAsync(lang, mode, cityId, priceRange, propertyType, keyword);

            var all = (raw ?? new List<PropertyListViewModel>())
                        .Select(x => MapToCardItem(x, mode))
                        .ToList();

            var ordered = OrderByFeaturedThenDate(all);

            var total = ordered.Count;
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages <= 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var pageItems = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            await ApplyFavoritesAsync(pageItems, lang);

            var vm = new HomeNow.ViewModels.PropertyListPageViewModel
            {
                Mode = mode,
                CityId = cityId,
                PriceRange = priceRange,
                PropertyType = propertyType,
                Keyword = keyword,

                Items = pageItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                Title = mode == "rent" ? "Tất cả căn cho thuê" : "Tất cả căn bán"
            };

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

            var raw = await _propertyService.SearchAsync(lang, mode, cityId, priceRange, propertyType, keyword);

            var all = (raw ?? new List<PropertyListViewModel>())
                        .Select(x => MapToCardItem(x, mode))
                        .ToList();

            var ordered = OrderByFeaturedThenDate(all);

            var total = ordered.Count;
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages <= 0) totalPages = 1;

            if (page > totalPages)
                return Content("");

            var pageItems = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            await ApplyFavoritesAsync(pageItems, lang);

            return PartialView("~/Views/Property/_PropertyRowList.cshtml", pageItems);
        }

        // ================= helpers =================

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

        private PropertyListItemViewModel MapToCardItem(PropertyListViewModel src, string fallbackListingType)
        {
            var listingType = GetString(src, "ListingType", fallbackListingType);
            if (string.IsNullOrWhiteSpace(listingType)) listingType = fallbackListingType;

            // ✅ FIX: Id của VM là "Id", không phải PropertyId
            var pid = GetInt(src, "PropertyId", GetInt(src, "Id", 0));

            var price = GetDecimal(src, "Price", 0m);
            var priceLabel = GetString(src, "PriceLabel", "");

            if (string.IsNullOrWhiteSpace(priceLabel) && price > 0m)
                priceLabel = FormatPriceLabel(price, listingType);

            // ✅ FIX: ưu tiên AreaSqm, fallback AreaM2
            var area = GetNullableFloat(src, "AreaSqm", null) ?? GetNullableFloat(src, "Area", null) ?? 0f;
            if (area <= 0f)
            {
                var areaM2 = GetNullableDecimal(src, "AreaM2", null);
                if (areaM2.HasValue) area = (float)areaM2.Value;
            }

            return new PropertyListItemViewModel
            {
                PropertyId = pid,

                Title = GetString(src, "Title", ""),
                Address = GetString(src, "Address", GetString(src, "AddressLine", "")),

                Price = price,
                PriceLabel = priceLabel,

                Bed = GetNullableInt(src, "BedroomCount", null),
                Bath = GetNullableInt(src, "BathroomCount", null),
                Area = area,

                ThumbnailUrl = GetString(src, "ThumbnailUrl", GetString(src, "CoverImageUrl", "")),

                ListingType = listingType,
                PropertyType = GetString(src, "PropertyType", ""),

                IsFavorite = GetBool(src, "IsFavorite", false)
            };
        }

        private List<PropertyListItemViewModel> OrderByFeaturedThenDate(List<PropertyListItemViewModel> items)
        {
            if (items == null || items.Count == 0)
                return items ?? new List<PropertyListItemViewModel>();

            var ids = items.Select(x => x.PropertyId).Distinct().ToList();

            Dictionary<int, (int featured, DateTime created)> meta;
            using (var db = new AppDbContext())
            {
                meta = db.Properties
                    .Where(p => ids.Contains(p.PropertyId))
                    .Select(p => new
                    {
                        p.PropertyId,
                        Featured = (p.IsFeatured ?? 0),
                        Created = (p.CreatedAt ?? DateTime.MinValue)
                    })
                    .ToList()
                    .ToDictionary(x => x.PropertyId, x => (x.Featured, x.Created));
            }

            return items
                .OrderByDescending(x => meta.TryGetValue(x.PropertyId, out var m) ? m.featured : 0)
                .ThenByDescending(x => meta.TryGetValue(x.PropertyId, out var m) ? m.created : DateTime.MinValue)
                .ThenByDescending(x => x.PropertyId)
                .ToList();
        }

        private async Task ApplyFavoritesAsync(List<PropertyListItemViewModel> pageItems, string langCode)
        {
            if (pageItems == null || pageItems.Count == 0) return;

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                pageItems.ForEach(x => x.IsFavorite = false);
                return;
            }

            var favList = await _favoriteService.GetFavoritesAsync(userId.Value, langCode);
            var favSet = new HashSet<int>(favList.Select(x => x.PropertyId));

            foreach (var item in pageItems)
                item.IsFavorite = favSet.Contains(item.PropertyId);
        }

        private string FormatPriceLabel(decimal price, string listingType)
        {
            var vi = CultureInfo.GetCultureInfo("vi-VN");
            var s = price.ToString("#,0", vi) + " ";
            if ((listingType ?? "").ToLower() == "rent")
                s += "/tháng";
            return s;
        }

        // ===== reflection safe getter =====

        private static string GetString(object obj, string prop, string def)
        {
            var v = GetPropValue(obj, prop);
            return v == null ? def : v.ToString();
        }

        private static int GetInt(object obj, string prop, int def)
        {
            var v = GetPropValue(obj, prop);
            if (v == null) return def;
            if (v is int i) return i;
            return int.TryParse(v.ToString(), out var n) ? n : def;
        }

        private static int? GetNullableInt(object obj, string prop, int? def)
        {
            var v = GetPropValue(obj, prop);
            if (v == null) return def;
            if (v is int i) return i;
            return int.TryParse(v.ToString(), out var n) ? n : def;
        }

        private static float? GetNullableFloat(object obj, string prop, float? def)
        {
            var v = GetPropValue(obj, prop);
            if (v == null) return def;
            if (v is float f) return f;
            if (v is double d) return (float)d;
            return float.TryParse(v.ToString(), out var n) ? n : def;
        }

        private static decimal GetDecimal(object obj, string prop, decimal def)
        {
            var v = GetPropValue(obj, prop);
            if (v == null) return def;
            if (v is decimal m) return m;
            return decimal.TryParse(v.ToString(), out var n) ? n : def;
        }

        private static decimal? GetNullableDecimal(object obj, string prop, decimal? def)
        {
            var v = GetPropValue(obj, prop);
            if (v == null) return def;
            if (v is decimal m) return m;
            return decimal.TryParse(v.ToString(), out var n) ? n : def;
        }

        private static bool GetBool(object obj, string prop, bool def)
        {
            var v = GetPropValue(obj, prop);
            if (v == null) return def;
            if (v is bool b) return b;
            return bool.TryParse(v.ToString(), out var n) ? n : def;
        }

        private static object GetPropValue(object obj, string prop)
        {
            if (obj == null) return null;
            var pi = obj.GetType().GetProperty(prop);
            return pi == null ? null : pi.GetValue(obj, null);
        }
    }
}
