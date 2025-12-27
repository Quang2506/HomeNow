using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

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
        private readonly IRedisCacheService _cache;

        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();
        private static readonly object SerializerLock = new object();

        private static readonly TimeSpan DropdownTtl = TimeSpan.FromHours(12);
        private static readonly TimeSpan FavMarkerTtl = TimeSpan.FromHours(6);

        // Detail cache (không user-specific)
        private static readonly TimeSpan DetailTtl = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan SimilarTtl = TimeSpan.FromMinutes(15);

        // UI yêu cầu 3 căn tương tự
        private const int SimilarTake = 3;

        public PropertyController()
        {
            _propertyService = new PropertyService();
            _favoriteService = new FavoriteService();
            _cache = new RedisCacheService();
        }

        // ------------------ Common ------------------
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

        private string NormalizeMode(string mode)
        {
            var m = (mode ?? "rent").Trim().ToLower();
            return (m == "sale") ? "sale" : "rent";
        }

        // RedisCacheService của bạn best-effort => không check IsAvailable để tránh block
        private bool CacheEnabled => _cache != null && _cache.IsEnabled;

        private static string NormalizeKeyPart(string s, string fallback = "unknown")
        {
            s = (s ?? "").Trim().ToLower();
            return string.IsNullOrWhiteSpace(s) ? fallback : s;
        }

        // ------------------ Cache Keys ------------------
        private static string FavIdsKey(int userId) => $"hn:fav:ids:{userId}";
        private static string FavMarkerKey(int userId) => $"hn:fav:marker:{userId}"; // COUNT string

        private static string CitiesKey(string lang) => $"hn:dd:cities:{lang}";
        private static string PricesKey(string lang) => $"hn:dd:prices:{lang}";
        private static string TypesKey(string lang) => $"hn:dd:types:{lang}";

        private static string DetailKey(string lang, int id) => $"hn:prop:detail:{lang}:{id}";

        // ✅ Similar cache key theo "bucket giá" (Cách 2)
        private static string SimilarKey(
            string lang,
            int? cityId,
            string listingType,
            string propertyType,
            long priceBucket,
            bool relaxed)
            => $"hn:prop:similar:{lang}:{cityId}:{NormalizeKeyPart(listingType)}:{NormalizeKeyPart(propertyType)}:pb:{priceBucket}:relax:{(relaxed ? 1 : 0)}";

        // ✅ Tier 2: cùng city + listingType + propertyType (KHÔNG giá)
        private static string SimilarKeyCityType(string lang, int? cityId, string listingType, string propertyType)
            => $"hn:prop:similar:ct:{lang}:{cityId}:{NormalizeKeyPart(listingType)}:{NormalizeKeyPart(propertyType)}";

        // ✅ Tier 3: cùng city + listingType (giữ rent/sale, bỏ propertyType)
        private static string SimilarKeyCityListing(string lang, int? cityId, string listingType)
            => $"hn:prop:similar:cl:{lang}:{cityId}:{NormalizeKeyPart(listingType)}";

        // per-request memoization keys
        private static string ReqFavIdsKey(int userId) => $"__req:fav:ids:{userId}";
        private static string ReqFavCountKey(int userId) => $"__req:fav:count:{userId}";

        // ------------------ JSON Cache Helpers ------------------
        private async Task<T> CacheGetJsonAsync<T>(string key) where T : class
        {
            if (!CacheEnabled) return null;

            try
            {
                var s = await _cache.GetStringAsync(key);
                if (string.IsNullOrWhiteSpace(s)) return null;

                lock (SerializerLock)
                {
                    return Serializer.Deserialize<T>(s);
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task CacheSetJsonAsync<T>(string key, T value, TimeSpan ttl) where T : class
        {
            if (!CacheEnabled || value == null) return;

            try
            {
                string s;
                lock (SerializerLock) { s = Serializer.Serialize(value); }
                await _cache.SetStringAsync(key, s, ttl);
            }
            catch
            {
                // ignore
            }
        }

        // ------------------ Favorites Cache ------------------
        private static int ParseMarkerCount(string marker)
        {
            if (string.IsNullOrWhiteSpace(marker)) return -1;
            return int.TryParse(marker, out var n) ? n : -1;
        }

        private async Task<int[]> GetFavoriteIdsCachedAsync(int userId, string lang)
        {
            var reqKey = ReqFavIdsKey(userId);
            if (HttpContext?.Items[reqKey] is int[] cachedIds)
                return cachedIds;

            var setKey = FavIdsKey(userId);
            var markerKey = FavMarkerKey(userId);

            // 1) Redis best-effort
            if (CacheEnabled)
            {
                try
                {
                    var marker = await _cache.GetStringAsync(markerKey);
                    var markerCount = ParseMarkerCount(marker);

                    if (markerCount == 0)
                    {
                        var empty = Array.Empty<int>();
                        if (HttpContext != null) HttpContext.Items[reqKey] = empty;
                        return empty;
                    }

                    // marker > 0 => ưu tiên đọc set
                    if (markerCount > 0)
                    {
                        var ids = await _cache.GetSetMembersIntAsync(setKey);
                        if (ids != null && ids.Length > 0)
                        {
                            if (HttpContext != null) HttpContext.Items[reqKey] = ids;
                            return ids;
                        }
                        // marker nói có mà set rỗng => rơi xuống DB rebuild
                    }
                    else
                    {
                        // marker chưa có => thử đọc set
                        var ids = await _cache.GetSetMembersIntAsync(setKey);
                        if (ids != null && ids.Length > 0)
                        {
                            await _cache.SetStringAsync(markerKey, ids.Length.ToString(), FavMarkerTtl);
                            if (HttpContext != null) HttpContext.Items[reqKey] = ids;
                            return ids;
                        }
                    }
                }
                catch
                {
                    // ignore => fallback DB
                }
            }

            // 2) DB fallback
            var favList = await _favoriteService.GetFavoritesAsync(userId, lang);
            var idsDb = (favList ?? new List<PropertyListItemViewModel>())
                .Select(x => x.PropertyId)
                .Distinct()
                .ToArray();

            // write back best-effort
            if (CacheEnabled)
            {
                try
                {
                    await _cache.ReplaceSetAsync(setKey, idsDb);
                    await _cache.SetStringAsync(markerKey, idsDb.Length.ToString(), FavMarkerTtl);
                }
                catch { /* ignore */ }
            }

            if (HttpContext != null) HttpContext.Items[reqKey] = idsDb;
            return idsDb;
        }

        private async Task<HashSet<int>> GetFavoriteSetCachedAsync(int? userId, string lang)
        {
            if (!userId.HasValue) return null;
            var ids = await GetFavoriteIdsCachedAsync(userId.Value, lang);
            return new HashSet<int>(ids ?? Array.Empty<int>());
        }

        private async Task<int> GetFavoriteCountCachedAsync(int? userId, string lang)
        {
            if (!userId.HasValue) return 0;

            var uid = userId.Value;
            var reqKey = ReqFavCountKey(uid);
            if (HttpContext?.Items[reqKey] is int cachedCount)
                return cachedCount;

            var setKey = FavIdsKey(uid);
            var markerKey = FavMarkerKey(uid);

            if (CacheEnabled)
            {
                try
                {
                    var marker = await _cache.GetStringAsync(markerKey);
                    var markerCount = ParseMarkerCount(marker);

                    if (markerCount == 0)
                    {
                        if (HttpContext != null) HttpContext.Items[reqKey] = 0;
                        return 0;
                    }

                    // SCARD nhanh
                    var len = await _cache.GetSetLengthAsync(setKey);
                    if (len > 0)
                    {
                        var n = (int)len;
                        if (HttpContext != null) HttpContext.Items[reqKey] = n;
                        return n;
                    }

                    // marker >0 mà len==0 => rebuild bằng ids
                    if (markerCount > 0)
                    {
                        var ids = await GetFavoriteIdsCachedAsync(uid, lang);
                        var n = ids.Length;
                        if (HttpContext != null) HttpContext.Items[reqKey] = n;
                        return n;
                    }
                }
                catch
                {
                    // ignore => fallback DB
                }
            }

            var idsDb = await GetFavoriteIdsCachedAsync(uid, lang);
            var countDb = idsDb.Length;
            if (HttpContext != null) HttpContext.Items[reqKey] = countDb;
            return countDb;
        }

        // ------------------ Favorite Endpoints ------------------
        [HttpPost]
        public async Task<ActionResult> ToggleFavorite(int propertyId = 0, int id = 0)
        {
            if (propertyId <= 0) propertyId = id;

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Json(new { success = false, needLogin = true }, JsonRequestBehavior.DenyGet);

            // 1) DB update
            var isFav = await _favoriteService.ToggleFavoriteAsync(userId.Value, propertyId);

            // 2) Redis update best-effort
            int favoriteCount;
            if (CacheEnabled)
            {
                var setKey = FavIdsKey(userId.Value);
                var markerKey = FavMarkerKey(userId.Value);

                try
                {
                    if (isFav) await _cache.AddToSetAsync(setKey, propertyId);
                    else await _cache.RemoveFromSetAsync(setKey, propertyId);

                    var len = (int)await _cache.GetSetLengthAsync(setKey);
                    await _cache.SetStringAsync(markerKey, len.ToString(), FavMarkerTtl);
                    favoriteCount = len;
                }
                catch
                {
                    // fallback count
                    var lang = GetLang2();
                    favoriteCount = await GetFavoriteCountCachedAsync(userId.Value, lang);
                }
            }
            else
            {
                var lang = GetLang2();
                favoriteCount = await GetFavoriteCountCachedAsync(userId.Value, lang);
            }

            // clear per-request memoize
            HttpContext?.Items.Remove(ReqFavIdsKey(userId.Value));
            HttpContext?.Items.Remove(ReqFavCountKey(userId.Value));

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
            ViewBag.FavoriteCount = await GetFavoriteCountCachedAsync(userId, lang);

            var list = await _favoriteService.GetFavoritesAsync(userId.Value, lang);
            return View(list);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> HeaderFavoriteInfo()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Json(new { isAuth = false, favoriteCount = 0, favoriteIds = new int[0] }, JsonRequestBehavior.AllowGet);

            var lang = GetLang2();
            var ids = await GetFavoriteIdsCachedAsync(userId.Value, lang);

            return Json(new
            {
                isAuth = true,
                favoriteCount = ids.Length,
                favoriteIds = ids
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [AllowAnonymous]
        public Task<ActionResult> FavoriteSummary() => HeaderFavoriteInfo();

        // ------------------ DETAIL + VR ------------------

        // ✅ bucket step cho Cách 2:
        // - rent: 1,000,000 VND
        // - sale: 100,000,000 VND
        private static decimal GetBucketStep(string listingType)
        {
            return string.Equals((listingType ?? "").Trim(), "rent", StringComparison.OrdinalIgnoreCase)
                ? 1_000_000m
                : 100_000_000m;
        }

        private static long ToPriceBucket(decimal price, decimal step)
        {
            if (step <= 0m) step = 1m;
            if (price <= 0m) return 0;
            // làm tròn về bucket gần nhất
            var k = (long)Math.Round(price / step, MidpointRounding.AwayFromZero);
            return k;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> Detail(int id)
        {
            var lang = GetLang2();
            var userId = GetCurrentUserId();

            //  để header hiển thị số tim ngay lúc render (không phải đợi ajax)
            ViewBag.FavoriteCount = await GetFavoriteCountCachedAsync(userId, lang);

            // detail cache (user-agnostic)
            PropertyDetailViewModel vm = await CacheGetJsonAsync<PropertyDetailViewModel>(DetailKey(lang, id));

            if (vm == null)
            {
                using (var db = new AppDbContext())
                {
                    var p = await db.Properties.AsNoTracking()
                        .Include(x => x.Translations)
                        .FirstOrDefaultAsync(x => x.PropertyId == id);

                    if (p == null) return HttpNotFound();
                    if (!string.Equals(p.Status, "published", StringComparison.OrdinalIgnoreCase))
                        return HttpNotFound();

                    var tr = (p.Translations ?? new List<PropertyTranslation>())
                        .FirstOrDefault(t => t.LangCode == lang);

                    var title = (tr != null && !string.IsNullOrEmpty(tr.DisplayTitle)) ? tr.DisplayTitle
                              : (tr != null && !string.IsNullOrEmpty(tr.Title)) ? tr.Title
                              : p.Title;

                    var addr = (tr != null && !string.IsNullOrEmpty(tr.AddressLine)) ? tr.AddressLine : p.AddressLine;

                    // City
                    string cityName = "";
                    if (p.CityId.HasValue)
                    {
                        var city = await db.Cities.AsNoTracking()
                            .FirstOrDefaultAsync(c => c.CityId == p.CityId.Value);

                        if (city != null)
                        {
                            cityName = lang == "en" ? (city.NameEn ?? city.NameVi)
                                     : lang == "zh" ? (city.NameZh ?? city.NameVi)
                                     : city.NameVi;
                        }
                    }

                    var price = p.Price ?? 0m;
                    var priceLabel = price > 0m ? FormatPriceLabel(price, p.ListingType) : "";

                    // Map
                    var mapQuery = $"{addr}, {cityName}".Trim().Trim(',');
                    var mapEmbedUrl = BuildMapEmbedUrl(mapQuery);
                    var mapClickUrl = BuildMapDirectionsUrl(mapQuery);

                    // ===================== SIMILAR (đủ 3 theo 3-tier) =====================
                    var listingTypeKey = NormalizeKeyPart(p.ListingType);
                    var propertyTypeKey = NormalizeKeyPart(p.PropertyType);

                    var step = GetBucketStep(p.ListingType);
                    var bucket = ToPriceBucket(price, step);

                    var dict = new Dictionary<int, PropertyListItemViewModel>();

                    // Tier 1: city + listingType + propertyType + approx price
                    if (price > 0m)
                    {
                        var k1 = SimilarKey(lang, p.CityId, listingTypeKey, propertyTypeKey, bucket, relaxed: false);
                        var tier1 = await CacheGetJsonAsync<List<PropertyListItemViewModel>>(k1);

                        if (tier1 == null)
                        {
                            tier1 = await QuerySimilarTierAsync(
                                db: db,
                                lang: lang,
                                current: p,
                                filterListingType: true,
                                filterPropertyType: true,
                                filterApproxPrice: true,
                                basePrice: price,
                                step: step,
                                bucket: bucket,
                                take: SimilarTake * 4);

                            await CacheSetJsonAsync(k1, tier1, SimilarTtl);
                        }

                        foreach (var x in (tier1 ?? new List<PropertyListItemViewModel>()))
                        {
                            if (dict.Count >= SimilarTake) break;
                            if (!dict.ContainsKey(x.PropertyId)) dict.Add(x.PropertyId, x);
                        }
                    }

                    // Tier 2: city + listingType + propertyType (no price)
                    if (dict.Count < SimilarTake)
                    {
                        var k2 = SimilarKeyCityType(lang, p.CityId, listingTypeKey, propertyTypeKey);
                        var tier2 = await CacheGetJsonAsync<List<PropertyListItemViewModel>>(k2);

                        if (tier2 == null)
                        {
                            tier2 = await QuerySimilarTierAsync(
                                db: db,
                                lang: lang,
                                current: p,
                                filterListingType: true,
                                filterPropertyType: true,
                                filterApproxPrice: false,
                                basePrice: price,
                                step: step,
                                bucket: bucket,
                                take: SimilarTake * 6);

                            await CacheSetJsonAsync(k2, tier2, SimilarTtl);
                        }

                        foreach (var x in (tier2 ?? new List<PropertyListItemViewModel>()))
                        {
                            if (dict.Count >= SimilarTake) break;
                            if (!dict.ContainsKey(x.PropertyId)) dict.Add(x.PropertyId, x);
                        }
                    }

                    // Tier 3: city + listingType (no propertyType)
                    if (dict.Count < SimilarTake)
                    {
                        var k3 = SimilarKeyCityListing(lang, p.CityId, listingTypeKey);
                        var tier3 = await CacheGetJsonAsync<List<PropertyListItemViewModel>>(k3);

                        if (tier3 == null)
                        {
                            tier3 = await QuerySimilarTierAsync(
                                db: db,
                                lang: lang,
                                current: p,
                                filterListingType: true,
                                filterPropertyType: false,
                                filterApproxPrice: false,
                                basePrice: price,
                                step: step,
                                bucket: bucket,
                                take: SimilarTake * 8);

                            await CacheSetJsonAsync(k3, tier3, SimilarTtl);
                        }

                        foreach (var x in (tier3 ?? new List<PropertyListItemViewModel>()))
                        {
                            if (dict.Count >= SimilarTake) break;
                            if (!dict.ContainsKey(x.PropertyId)) dict.Add(x.PropertyId, x);
                        }
                    }

                    var similar = dict.Values.ToList();

                    // ưu tiên “gần giá” nếu có giá, sau đó giữ thứ tự featured/created/id
                    if (price > 0m && similar.Count > 1)
                    {
                        similar = similar
                            .OrderBy(x => (x.Price > 0m) ? Math.Abs(x.Price - price) : decimal.MaxValue)
                            .ThenByDescending(x => x.Price > 0m)
                            .ThenByDescending(x => x.PropertyId)
                            .Take(SimilarTake)
                            .ToList();
                    }
                    else
                    {
                        similar = similar.Take(SimilarTake).ToList();
                    }
                    // ===================== END SIMILAR =====================

                    vm = new PropertyDetailViewModel
                    {
                        PropertyId = p.PropertyId,
                        Title = title,
                        AddressLine = addr,
                        CityName = cityName,

                        ListingType = p.ListingType,
                        PropertyType = p.PropertyType,

                        Price = price,
                        PriceLabel = priceLabel,

                        Area = p.AreaSqm ?? 0f,
                        BedroomCount = p.BedroomCount,
                        BathroomCount = p.BathroomCount,

                        IsVrAvailable = p.IsVrAvailable ?? false,
                        CoverImageUrl = p.CoverImageUrl,

                        Description = p.Description,
                        Amenities = ParseStringList(p.Amenities),

                        MapEmbedUrl = mapEmbedUrl,
                        MapClickUrl = mapClickUrl,

                        Similar = similar ?? new List<PropertyListItemViewModel>()
                    };

                    await CacheSetJsonAsync(DetailKey(lang, id), vm, DetailTtl);
                }
            }

            // set fav flag for similar
            if (userId.HasValue && vm?.Similar != null && vm.Similar.Count > 0)
            {
                var favSet = await GetFavoriteSetCachedAsync(userId, lang);
                if (favSet != null)
                {
                    foreach (var s in vm.Similar)
                        s.IsFavorite = favSet.Contains(s.PropertyId);
                }
            }

            return View(vm);
        }

        // ✅ Query theo tier (dùng chung cho tier1/2/3, không đụng logic khác)
        private async Task<List<PropertyListItemViewModel>> QuerySimilarTierAsync(
            AppDbContext db,
            string lang,
            Property current,
            bool filterListingType,
            bool filterPropertyType,
            bool filterApproxPrice,
            decimal basePrice,
            decimal step,
            long bucket,
            int take)
        {
            var q =
                from pp in db.Properties.AsNoTracking()
                join tr2 in db.PropertyTranslations.AsNoTracking()
                    on new { Id = pp.PropertyId, Lang = lang }
                    equals new { Id = tr2.PropertyId, Lang = tr2.LangCode }
                    into gj
                from tr2 in gj.DefaultIfEmpty()
                where pp.Status == "published"
                   && pp.PropertyId != current.PropertyId
                   && pp.CityId == current.CityId
                select new { pp, tr2 };

            if (filterListingType)
                q = q.Where(x => x.pp.ListingType == current.ListingType);

            if (filterPropertyType)
                q = q.Where(x => x.pp.PropertyType == current.PropertyType);

            if (filterApproxPrice && basePrice > 0m)
            {
                var bucketValue = bucket * step;
                var min = Math.Max(0m, bucketValue - (2m * step));
                var max = bucketValue + (2m * step);

                q = q.Where(x => x.pp.Price.HasValue
                              && x.pp.Price.Value >= min
                              && x.pp.Price.Value <= max);
            }

            var rows = await q
                .OrderByDescending(x => x.pp.IsFeatured)
                .ThenByDescending(x => x.pp.CreatedAt)
                .ThenByDescending(x => x.pp.PropertyId)
                .Take(Math.Max(60, take))
                .ToListAsync();

            var mapped = rows.Select(x =>
            {
                var pp = x.pp;
                var tr2 = x.tr2;

                var tTitle = (tr2 != null && !string.IsNullOrEmpty(tr2.DisplayTitle)) ? tr2.DisplayTitle
                           : (tr2 != null && !string.IsNullOrEmpty(tr2.Title)) ? tr2.Title
                           : pp.Title;

                var tAddr = (tr2 != null && !string.IsNullOrEmpty(tr2.AddressLine)) ? tr2.AddressLine : pp.AddressLine;

                var price = pp.Price ?? 0m;

                // tránh lỗi kiểu ?? giữa int?/bool (CS0019)
                var isFeaturedBool = (pp.IsFeatured ?? 0) > 0;
                var createdAt = pp.CreatedAt ?? DateTime.MinValue;

                return new
                {
                    Item = new PropertyListItemViewModel
                    {
                        PropertyId = pp.PropertyId,
                        Title = tTitle,
                        Address = tAddr,
                        ThumbnailUrl = pp.CoverImageUrl,
                        Bed = pp.BedroomCount,
                        Bath = pp.BathroomCount,
                        Area = pp.AreaSqm ?? 0f,

                        Price = price,
                        PriceLabel = (price > 0m) ? FormatPriceLabel(price, pp.ListingType) : "",

                        ListingType = pp.ListingType,
                        PropertyType = pp.PropertyType,
                        IsFavorite = false
                    },
                    IsFeatured = isFeaturedBool,
                    CreatedAt = createdAt
                };
            }).ToList();

            if (basePrice > 0m)
            {
                mapped = mapped
                    .OrderBy(x => (x.Item.Price > 0m) ? Math.Abs(x.Item.Price - basePrice) : decimal.MaxValue)
                    .ThenByDescending(x => x.IsFeatured)
                    .ThenByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Item.PropertyId)
                    .ToList();
            }
            else
            {
                mapped = mapped
                    .OrderByDescending(x => x.IsFeatured)
                    .ThenByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Item.PropertyId)
                    .ToList();
            }

            return mapped.Select(x => x.Item).Take(take).ToList();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> Vr(int id)
        {
            var lang = GetLang2();

            using (var db = new AppDbContext())
            {
                var p = await db.Properties.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.PropertyId == id);

                if (p == null) return HttpNotFound();
                if (!string.Equals(p.Status, "published", StringComparison.OrdinalIgnoreCase))
                    return HttpNotFound();

                if (!(p.IsVrAvailable ?? false))
                    return RedirectToAction("Detail", new { id });

                var scenes = await db.VrScenes
                    .Where(s => s.PropertyId == id)
                    .Include(s => s.Hotspots.Select(h => h.Translations))
                    .Include(s => s.Translations)
                    .ToListAsync();

                if (scenes == null || scenes.Count == 0)
                    return RedirectToAction("Detail", new { id });

                ViewBag.PropertyId = id;

                var vm = new HomeNow.ViewModels.VrViewModel
                {
                    Lang = lang,
                    Scenes = scenes
                };

                return View("Vr", vm);
            }
        }

        // ------------------ LIST + LOADMORE ------------------
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
                HasSearch = true,
                Page = page,
                PageSize = pageSize
            };

            await FillDropDownAndHeroCachedAsync(vm, lang);

            var userId = GetCurrentUserId();
            var favoriteSet = await GetFavoriteSetCachedAsync(userId, lang);
            ViewBag.FavoriteCount = await GetFavoriteCountCachedAsync(userId, lang);

            var pr = await _propertyService.SearchPagedAsync(lang, mode, cityId, priceRange, propertyType, keyword, page, pageSize);

            vm.TotalItems = pr.TotalItems;
            vm.TotalPages = (int)Math.Ceiling(pr.TotalItems / (double)pageSize);
            if (vm.TotalPages <= 0) vm.TotalPages = 1;
            if (vm.Page > vm.TotalPages) vm.Page = vm.TotalPages;

            vm.SearchResults = (pr.Items ?? new List<PropertyListViewModel>())
                .Select(x => ToHomeItem(x, favoriteSet, mode))
                .ToList();

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
            var favoriteSet = await GetFavoriteSetCachedAsync(userId, lang);

            var pr = await _propertyService.SearchPagedAsync(lang, mode, cityId, priceRange, propertyType, keyword, page, pageSize);

            var totalPages = (int)Math.Ceiling(pr.TotalItems / (double)pageSize);
            if (totalPages <= 0) totalPages = 1;
            if (page > totalPages) return Content("");

            var list = (pr.Items ?? new List<PropertyListViewModel>())
                .Select(x => ToHomeItem(x, favoriteSet, mode))
                .ToList();

            return PartialView("~/Views/Home/_PropertyRowList.cshtml", list);
        }

        // ------------------ DROPDOWN CACHE ------------------
        private async Task FillDropDownAndHeroCachedAsync(HomeIndexViewModel vm, string lang)
        {
            List<CityDropDownItem> cities = null;
            List<PriceFilterDropDownItem> prices = null;
            List<PropertyTypeDropDownItem> types = null;

            if (CacheEnabled)
            {
                var tCities = CacheGetJsonAsync<List<CityDropDownItem>>(CitiesKey(lang));
                var tPrices = CacheGetJsonAsync<List<PriceFilterDropDownItem>>(PricesKey(lang));
                var tTypes = CacheGetJsonAsync<List<PropertyTypeDropDownItem>>(TypesKey(lang));
                await Task.WhenAll(tCities, tPrices, tTypes);

                cities = tCities.Result;
                prices = tPrices.Result;
                types = tTypes.Result;
            }

            if (cities != null && prices != null && types != null)
            {
                vm.Cities = cities;
                vm.PriceFilters = prices;
                vm.PropertyTypes = types;

                var cityForBg = vm.CityId.HasValue
                    ? (vm.Cities ?? new List<CityDropDownItem>()).FirstOrDefault(c => c.CityId == vm.CityId.Value)
                    : (vm.Cities ?? new List<CityDropDownItem>()).FirstOrDefault();

                ViewBag.HeroBackground = cityForBg?.BackgroundUrl ?? "/Assets/Cities/Banner.png";
                return;
            }

            using (var db = new AppDbContext())
            {
                var cList = await db.Cities.AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                vm.Cities = cList.Select(c => new CityDropDownItem
                {
                    CityId = c.CityId,
                    Name = lang == "en" ? (c.NameEn ?? c.NameVi)
                         : lang == "zh" ? (c.NameZh ?? c.NameVi)
                         : c.NameVi,
                    BackgroundUrl = c.BackgroundUrl
                }).ToList();

                var pList = await db.PriceFilters.AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.DisplayOrder)
                    .ToListAsync();

                vm.PriceFilters = pList.Select(p => new PriceFilterDropDownItem
                {
                    Code = p.Code,
                    Name = lang == "en" ? (p.NameEn ?? p.NameVi)
                         : lang == "zh" ? (p.NameZh ?? p.NameVi)
                         : p.NameVi,
                    MinPrice = p.MinPrice,
                    MaxPrice = p.MaxPrice
                }).ToList();

                var tList = await db.PropertyTypes.AsNoTracking()
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.DisplayOrder)
                    .ToListAsync();

                vm.PropertyTypes = tList.Select(t => new PropertyTypeDropDownItem
                {
                    Code = t.Code,
                    Name = lang == "en" ? (t.NameEn ?? t.NameVi)
                         : lang == "zh" ? (t.NameZh ?? t.NameVi)
                         : t.NameVi
                }).ToList();

                var cityForBg = vm.CityId.HasValue
                    ? cList.FirstOrDefault(c => c.CityId == vm.CityId.Value)
                    : cList.FirstOrDefault();

                ViewBag.HeroBackground = cityForBg?.BackgroundUrl ?? "/Assets/Cities/Banner.png";
            }

            if (CacheEnabled)
            {
                await CacheSetJsonAsync(CitiesKey(lang), vm.Cities, DropdownTtl);
                await CacheSetJsonAsync(PricesKey(lang), vm.PriceFilters, DropdownTtl);
                await CacheSetJsonAsync(TypesKey(lang), vm.PropertyTypes, DropdownTtl);
            }
        }

        // ------------------ Mapper ------------------
        private PropertyListItemViewModel ToHomeItem(PropertyListViewModel x, ISet<int> favoriteIds, string fallbackListingType)
        {
            var listingType = string.IsNullOrWhiteSpace(x.ListingType) ? fallbackListingType : x.ListingType;

            var price = x.Price ?? 0m;
            var priceLabel = price > 0m ? FormatPriceLabel(price, listingType) : "";

            // map sau khi materialize -> tránh lỗi cast decimal trong EF
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
                ListingType = listingType,
                PropertyType = x.PropertyType,
                IsFavorite = favoriteIds != null && favoriteIds.Contains(x.Id)
            };
        }

        private string FormatPriceLabel(decimal price, string listingType)
        {
            var vi = CultureInfo.GetCultureInfo("vi-VN");
            var s = price.ToString("#,0", vi);

            if ((listingType ?? "").ToLower() == "rent")
                s += " /tháng";

            return s;
        }

        // ------------------ Detail helpers ------------------
        private static List<string> ParseStringList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            raw = raw.Trim();

            // support JSON ["wifi","ac"]
            try
            {
                if (raw.StartsWith("["))
                {
                    lock (SerializerLock)
                    {
                        var arr = Serializer.Deserialize<string[]>(raw);
                        return (arr ?? new string[0])
                            .Select(x => (x ?? "").Trim())
                            .Where(x => x.Length > 0)
                            .Distinct()
                            .ToList();
                    }
                }
            }
            catch { /* fallback */ }

            // CSV / newline
            return raw.Split(new[] { ',', ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct()
                .ToList();
        }

        // Map embed theo address_line
        private static string BuildMapEmbedUrl(string query)
        {
            var q = Uri.EscapeDataString(query ?? "");
            return $"https://www.google.com/maps?q={q}&output=embed";
        }

        // Click: Google Maps chỉ đường đến địa chỉ đó
        private static string BuildMapDirectionsUrl(string query)
        {
            var q = Uri.EscapeDataString(query ?? "");
            return $"https://www.google.com/maps/dir/?api=1&destination={q}&travelmode=driving";
        }
    }
}
