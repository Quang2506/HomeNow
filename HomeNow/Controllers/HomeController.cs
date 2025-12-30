using Core.Models;
using Core.ViewModels;
using Data;
using Services.Implementations;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;              // ✅ thêm để dùng ToArrayAsync / ToListAsync
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace HomeNow.Controllers
{
    public class HomeController : BaseController
    {
        private readonly IPropertyService _propertyService;
        private readonly IRedisCacheService _cache;

        private readonly JavaScriptSerializer _ser = new JavaScriptSerializer();

        private static readonly TimeSpan DropdownTtl = TimeSpan.FromHours(12);
        private static readonly TimeSpan FavMarkerTtl = TimeSpan.FromHours(6);
        private static readonly TimeSpan TotalCountTtl = TimeSpan.FromMinutes(5);

        public HomeController()
        {
            _propertyService = new PropertyService();
            _cache = new RedisCacheService();
        }

        private int? GetCurrentUserId()
        {
            if (Session["CurrentUserId"] is int id1) return id1;
            if (Session["CurrentUserId"] != null && int.TryParse(Session["CurrentUserId"].ToString(), out var parsed)) return parsed;

            if (Session["UserId"] is int id2) return id2;
            if (Session["UserId"] != null && int.TryParse(Session["UserId"].ToString(), out var parsed2)) return parsed2;

            return null;
        }

        private string GetLang2()
        {
            var uiLang = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            return uiLang.Length >= 2 ? uiLang.Substring(0, 2).ToLower() : "vi";
        }

        private bool CacheEnabled => _cache != null && _cache.IsEnabled;

        private static string CitiesKey(string lang) => $"hn:dd:cities:{lang}";
        private static string PricesKey(string lang) => $"hn:dd:prices:{lang}";
        private static string TypesKey(string lang) => $"hn:dd:types:{lang}";

        private static string FavIdsKey(int userId) => $"hn:fav:ids:{userId}";
        private static string FavMarkerKey(int userId) => $"hn:fav:marker:{userId}";

        private static string TotalPublishedKey() => "hn:stat:total:published";

        private async Task<T> CacheGetJsonAsync<T>(string key) where T : class
        {
            if (!CacheEnabled) return null;
            var s = await _cache.GetStringAsync(key).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(s)) return null;
            try { return _ser.Deserialize<T>(s); } catch { return null; }
        }

        private Task CacheSetJsonAsync<T>(string key, T value, TimeSpan ttl) where T : class
        {
            if (!CacheEnabled || value == null) return Task.CompletedTask;
            try
            {
                var s = _ser.Serialize(value);
                return _cache.SetStringAsync(key, s, ttl);
            }
            catch { return Task.CompletedTask; }
        }

        // ====================== FAVORITES (CHỈ SỬA PHẦN NÀY) ======================

        // marker mới: lưu COUNT dạng số (0, 1, 2...) để tránh bị hiểu nhầm như logic cũ ("1" => coi như empty)
        private static int ParseMarkerCount(string marker)
        {
            if (string.IsNullOrWhiteSpace(marker)) return -1; // chưa có marker
            return int.TryParse(marker, out var n) ? n : -1;
        }

        // Favorites: Redis set + marker(count) (đồng bộ với PropertyController)
        private async Task<int[]> GetFavoriteIdsCachedAsync(int userId, string lang)
        {
            var setKey = FavIdsKey(userId);
            var markerKey = FavMarkerKey(userId);

            // 1) Redis best-effort
            if (CacheEnabled)
            {
                try
                {
                    var marker = await _cache.GetStringAsync(markerKey).ConfigureAwait(false);
                    var markerCount = ParseMarkerCount(marker);

                    // marker = 0 => chắc chắn rỗng
                    if (markerCount == 0) return Array.Empty<int>();

                    if (markerCount > 0)
                    {
                        // marker nói có => ưu tiên đọc set
                        var ids = await _cache.GetSetMembersIntAsync(setKey).ConfigureAwait(false);
                        if (ids != null && ids.Length > 0) return ids;
                        // marker nói có nhưng set rỗng => rơi xuống DB rebuild
                    }
                    else
                    {
                        // chưa có marker => thử đọc set trước
                        var ids = await _cache.GetSetMembersIntAsync(setKey).ConfigureAwait(false);
                        if (ids != null && ids.Length > 0)
                        {
                            await _cache.SetStringAsync(markerKey, ids.Length.ToString(), FavMarkerTtl).ConfigureAwait(false);
                            return ids;
                        }
                    }
                }
                catch
                {
                    // ignore -> DB fallback
                }
            }

            // 2) DB fallback: public.favorites (status=1)
            int[] idsDb;
            using (var db = new AppDbContext())
            {
                idsDb = await db.Favorites.AsNoTracking()
                    .Where(f => f.UserId == userId && f.Status == 1)
                    .Select(f => f.PropertyId)
                    .Distinct()
                    .ToArrayAsync()
                    .ConfigureAwait(false);
            }

            // 3) write back cache best-effort
            if (CacheEnabled)
            {
                try
                {
                    await _cache.ReplaceSetAsync(setKey, idsDb).ConfigureAwait(false);
                    await _cache.SetStringAsync(markerKey, (idsDb?.Length ?? 0).ToString(), FavMarkerTtl).ConfigureAwait(false);
                }
                catch { /* ignore */ }
            }

            return idsDb ?? Array.Empty<int>();
        }

        private async Task<HashSet<int>> GetFavoriteSetCachedAsync(int? userId, string lang)
        {
            if (!userId.HasValue) return null;
            var ids = await GetFavoriteIdsCachedAsync(userId.Value, lang).ConfigureAwait(false);
            return new HashSet<int>(ids ?? Array.Empty<int>());
        }

        private async Task<int> GetFavoriteCountCachedAsync(int? userId, string lang)
        {
            if (!userId.HasValue) return 0;

            var uid = userId.Value;
            var setKey = FavIdsKey(uid);
            var markerKey = FavMarkerKey(uid);

            if (CacheEnabled)
            {
                try
                {
                    var marker = await _cache.GetStringAsync(markerKey).ConfigureAwait(false);
                    var markerCount = ParseMarkerCount(marker);

                    if (markerCount == 0) return 0;

                    // SCARD nhanh
                    var len = await _cache.GetSetLengthAsync(setKey).ConfigureAwait(false);
                    if (len > 0) return (int)len;

                    // marker>0 mà len==0 => rebuild
                    if (markerCount > 0)
                    {
                        var ids = await GetFavoriteIdsCachedAsync(uid, lang).ConfigureAwait(false);
                        return ids.Length;
                    }
                }
                catch
                {
                    // ignore -> DB fallback
                }
            }

            var idsDb = await GetFavoriteIdsCachedAsync(uid, lang).ConfigureAwait(false);
            return idsDb.Length;
        }

        // Favorites Popup (AJAX) - giờ đọc từ bảng favorites (status=1)
       

     [HttpGet]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public async Task<ActionResult> FavoritesPopup()
    {
        Response.Cache.SetCacheability(HttpCacheability.NoCache);
        Response.Cache.SetNoStore();
        Response.Cache.SetExpires(DateTime.UtcNow.AddSeconds(-1));

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Json(new { needLogin = true }, JsonRequestBehavior.AllowGet);

        var lang = GetLang2();
        var list = await QueryFavoriteCardsAsync(userId.Value, lang).ConfigureAwait(false);
        var result = list ?? new List<PropertyListItemViewModel>();

        return PartialView("_PropertyCardList", result);
    }


    private async Task<List<PropertyListItemViewModel>> QueryFavoriteCardsAsync(int userId, string lang)
        {
            using (var db = new AppDbContext())
            {
                var q =
                    from f in db.Favorites.AsNoTracking()
                    join p in db.Properties.AsNoTracking() on f.PropertyId equals p.PropertyId
                    join tr in db.PropertyTranslations.AsNoTracking().Where(t => t.LangCode == lang)
                        on p.PropertyId equals tr.PropertyId into gj
                    from tr in gj.DefaultIfEmpty()
                    where f.UserId == userId
                       && f.Status == 1
                       && p.Status == "published"
                    orderby f.CreatedAt descending
                    select new { p, tr };

                // dropdown/popup không nên quá nặng
                var rows = await q.Take(200).ToListAsync().ConfigureAwait(false);

                return rows.Select(x =>
                {
                    var p = x.p;
                    var tr = x.tr;

                    var title = (tr != null && !string.IsNullOrEmpty(tr.DisplayTitle)) ? tr.DisplayTitle
                              : (tr != null && !string.IsNullOrEmpty(tr.Title)) ? tr.Title
                              : p.Title;

                    var addr = (tr != null && !string.IsNullOrEmpty(tr.AddressLine)) ? tr.AddressLine : p.AddressLine;

                    var price = p.Price ?? 0m;
                    var priceLabel = price > 0m ? FormatPriceLabel(price, p.ListingType) : "";

                    return new PropertyListItemViewModel
                    {
                        PropertyId = p.PropertyId,
                        Title = title,
                        Address = addr,
                        ThumbnailUrl = p.CoverImageUrl,

                        Bed = p.BedroomCount,
                        Bath = p.BathroomCount,
                        Area = p.AreaSqm ?? 0f,

                        Price = price,
                        PriceLabel = priceLabel,

                        ListingType = p.ListingType,
                        PropertyType = p.PropertyType,

                        IsFavorite = true
                    };
                }).ToList();
            }
        }

        // ====================== HẾT FAVORITES (PHẦN KHÁC GIỮ NGUYÊN) ======================

        private async Task FillDropDownAndHeroCachedAsync(HomeIndexViewModel vm, string lang)
        {
            List<CityDropDownItem> cities = null;
            List<PriceFilterDropDownItem> prices = null;
            List<PropertyTypeDropDownItem> types = null;

            if (CacheEnabled)
            {
                var t1 = CacheGetJsonAsync<List<CityDropDownItem>>(CitiesKey(lang));
                var t2 = CacheGetJsonAsync<List<PriceFilterDropDownItem>>(PricesKey(lang));
                var t3 = CacheGetJsonAsync<List<PropertyTypeDropDownItem>>(TypesKey(lang));

                await Task.WhenAll(t1, t2, t3).ConfigureAwait(false);

                cities = t1.Result;
                prices = t2.Result;
                types = t3.Result;
            }

            if (cities != null && prices != null && types != null)
            {
                vm.Cities = cities;
                vm.PriceFilters = prices;
                vm.PropertyTypes = types;

                // ✅ FIX: Chỉ đổi nền khi user chọn city. Không chọn -> luôn Banner.jpg
                if (vm.CityId.HasValue)
                {
                    var cityForBg = vm.Cities.FirstOrDefault(c => c.CityId == vm.CityId.Value);
                    ViewBag.HeroBackground = cityForBg?.BackgroundUrl ?? "/Assets/Cities/Banner.png";
                }
                else
                {
                    ViewBag.HeroBackground = "/Assets/Cities/Banner.png";
                }

                return;
            }

            using (var db = new AppDbContext())
            {
                var cityEntities = db.Cities.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList();
                vm.Cities = cityEntities.Select(c => new CityDropDownItem
                {
                    CityId = c.CityId,
                    Name = lang == "en" ? (c.NameEn ?? c.NameVi)
                         : lang == "zh" ? (c.NameZh ?? c.NameVi)
                         : c.NameVi,
                    BackgroundUrl = c.BackgroundUrl
                }).ToList();

                var priceEntities = db.PriceFilters.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).ToList();
                vm.PriceFilters = priceEntities.Select(p => new PriceFilterDropDownItem
                {
                    Code = p.Code,
                    Name = lang == "en" ? (p.NameEn ?? p.NameVi)
                         : lang == "zh" ? (p.NameZh ?? p.NameVi)
                         : p.NameVi,
                    MinPrice = p.MinPrice,
                    MaxPrice = p.MaxPrice
                }).ToList();

                var typeEntities = db.PropertyTypes.Where(t => t.IsActive).OrderBy(t => t.DisplayOrder).ToList();
                vm.PropertyTypes = typeEntities.Select(t => new PropertyTypeDropDownItem
                {
                    Code = t.Code,
                    Name = lang == "en" ? (t.NameEn ?? t.NameVi)
                         : lang == "zh" ? (t.NameZh ?? t.NameVi)
                         : t.NameVi
                }).ToList();

                //
                if (vm.CityId.HasValue)
                {
                    var cityForBg = cityEntities.FirstOrDefault(c => c.CityId == vm.CityId.Value);
                    ViewBag.HeroBackground = cityForBg?.BackgroundUrl ?? "/Assets/Cities/Banner.png";
                }
                else
                {
                    ViewBag.HeroBackground = "/Assets/Cities/Banner.png";
                }
            }

            if (CacheEnabled)
            {
                await CacheSetJsonAsync(CitiesKey(lang), vm.Cities, DropdownTtl).ConfigureAwait(false);
                await CacheSetJsonAsync(PricesKey(lang), vm.PriceFilters, DropdownTtl).ConfigureAwait(false);
                await CacheSetJsonAsync(TypesKey(lang), vm.PropertyTypes, DropdownTtl).ConfigureAwait(false);
            }
        }

        private async Task<int> GetTotalPublishedCachedAsync()
        {
            if (CacheEnabled)
            {
                var s = await _cache.GetStringAsync(TotalPublishedKey()).ConfigureAwait(false);
                if (int.TryParse(s, out var n) && n >= 0) return n;
            }

            int total;
            using (var db = new AppDbContext())
            {
                total = db.Properties.AsNoTracking().Count(p => p.Status == "published");
            }

            if (CacheEnabled)
                await _cache.SetStringAsync(TotalPublishedKey(), total.ToString(), TotalCountTtl).ConfigureAwait(false);

            return total;
        }

        public async Task<ActionResult> Index(
            string transactionType,
            int? cityId,
            string priceRange,
            string propertyType,
            string keyword,
            int page = 1)
        {
            const int PageSize = 16;

            var lang = GetLang2();

            var vm = new HomeIndexViewModel
            {
                TransactionType = transactionType,
                CityId = cityId,
                PriceRange = priceRange,
                PropertyType = propertyType,
                Keyword = keyword,
                Page = page <= 0 ? 1 : page,
                PageSize = PageSize
            };

            // chạy song song để giảm “cảm giác chậm”
            var userId = GetCurrentUserId();
            var tDrop = FillDropDownAndHeroCachedAsync(vm, lang);
            var tTotal = GetTotalPublishedCachedAsync();
            var tFavSet = GetFavoriteSetCachedAsync(userId, lang);
            var tFavCount = GetFavoriteCountCachedAsync(userId, lang);

            await Task.WhenAll(tDrop, tTotal, tFavSet, tFavCount).ConfigureAwait(false);

            ViewBag.TotalListings = tTotal.Result;
            ViewBag.FavoriteCount = tFavCount.Result;

            var favoriteSet = tFavSet.Result;

            vm.HasSearch =
                !string.IsNullOrWhiteSpace(keyword) ||
                cityId.HasValue ||
                !string.IsNullOrWhiteSpace(priceRange) ||
                !string.IsNullOrWhiteSpace(propertyType) ||
                !string.IsNullOrWhiteSpace(transactionType);

            if (vm.HasSearch)
            {
                var listingType = string.IsNullOrWhiteSpace(transactionType) ? null : transactionType;

                var paged = await _propertyService.SearchPagedAsync(
                    lang, listingType, vm.CityId, vm.PriceRange, vm.PropertyType, vm.Keyword,
                    vm.Page, vm.PageSize).ConfigureAwait(false);

                vm.TotalItems = paged.TotalItems;
                vm.TotalPages = (int)Math.Ceiling(vm.TotalItems / (double)vm.PageSize);
                if (vm.TotalPages <= 0) vm.TotalPages = 1;
                if (vm.Page > vm.TotalPages) vm.Page = vm.TotalPages;

                vm.SearchResults = (paged.Items ?? new List<PropertyListViewModel>())
                    .Select(x => ToHomeItem(x, favoriteSet))
                    .ToList();
            }
            else
            {
                vm.TransactionType = null;
                vm.FeaturedRent = await GetFeaturedTop4DbAsync(lang, "rent", favoriteSet).ConfigureAwait(false);
                vm.FeaturedSale = await GetFeaturedTop4DbAsync(lang, "sale", favoriteSet).ConfigureAwait(false);
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<ActionResult> LoadMore(
            string transactionType,
            int? cityId,
            string priceRange,
            string propertyType,
            string keyword,
            int page = 2,
            bool clientFav = false)
        {
            const int PageSize = 16;
            var lang = GetLang2();

            HashSet<int> favoriteSet = null;

            if (!clientFav)
            {
                var userId = GetCurrentUserId();
                favoriteSet = await GetFavoriteSetCachedAsync(userId, lang).ConfigureAwait(false);
            }

            var listingType = string.IsNullOrWhiteSpace(transactionType) ? null : transactionType;

            var paged = await _propertyService.SearchPagedAsync(
                lang, listingType, cityId, priceRange, propertyType, keyword,
                page <= 0 ? 1 : page, PageSize).ConfigureAwait(false);

            var list = (paged.Items ?? new List<PropertyListViewModel>())
                .Select(x => ToHomeItem(x, favoriteSet))
                .ToList();

            if (list.Count == 0) return Content("");
            return PartialView("_PropertyCardList", list);
        }

        // Featured: lấy DB nhanh (top 4)
        private async Task<List<PropertyListItemViewModel>> GetFeaturedTop4DbAsync(string lang, string mode, ISet<int> favoriteSet)
        {
            var paged = await _propertyService.SearchPagedAsync(lang, mode, null, null, null, null, 1, 4).ConfigureAwait(false);
            return (paged.Items ?? new List<PropertyListViewModel>())
                .Select(x => ToHomeItem(x, favoriteSet))
                .Take(4)
                .ToList();
        }

        private PropertyListItemViewModel ToHomeItem(PropertyListViewModel x, ISet<int> favoriteIds)
        {
            var price = x.Price ?? 0m;
            var priceLabel = price > 0m ? FormatPriceLabel(price, x.ListingType) : "";

            var area = x.AreaSqm.HasValue ? (float)x.AreaSqm.Value : 0f;

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
            return price.ToString("#,0", vi);
        }
    }
}
