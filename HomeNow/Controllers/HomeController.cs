using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

using Core.Models;
using Core.ViewModels;
using Data;
using Services.Implementations;
using Services.Interfaces;

namespace HomeNow.Controllers
{
    public class HomeController : BaseController
    {
        private readonly IPropertyService _propertyService;
        private readonly IFavoriteService _favoriteService;

        public HomeController()
        {
            _propertyService = new PropertyService();
            _favoriteService = new FavoriteService();
        }

        private int? GetCurrentUserId()
        {
            if (Session["CurrentUserId"] is int id1)
                return id1;

            if (Session["CurrentUserId"] != null &&
                int.TryParse(Session["CurrentUserId"].ToString(), out var parsed))
                return parsed;

            if (Session["UserId"] is int id2)
                return id2;

            if (Session["UserId"] != null &&
                int.TryParse(Session["UserId"].ToString(), out var parsed2))
                return parsed2;

            return null;
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

            var uiLang = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            var lang = uiLang.Length >= 2 ? uiLang.Substring(0, 2).ToLower() : "vi";

            var vm = new HomeIndexViewModel
            {
                TransactionType = transactionType,
                CityId = cityId,
                PriceRange = priceRange,
                PropertyType = propertyType,
                Keyword = keyword,
                Page = page,
                PageSize = PageSize
            };

            using (var db = new AppDbContext())
            {
                var cities = db.Cities
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToList();

                vm.Cities = cities.Select(c => new CityDropDownItem
                {
                    CityId = c.CityId,
                    Name =
                        lang == "en" ? (c.NameEn ?? c.NameVi) :
                        lang == "zh" ? (c.NameZh ?? c.NameVi) :
                        c.NameVi,
                    BackgroundUrl = c.BackgroundUrl
                }).ToList();

                var priceFilters = db.PriceFilters
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.DisplayOrder)
                    .ToList();

                vm.PriceFilters = priceFilters.Select(p => new PriceFilterDropDownItem
                {
                    Code = p.Code,
                    Name =
                        lang == "en" ? (p.NameEn ?? p.NameVi) :
                        lang == "zh" ? (p.NameZh ?? p.NameVi) :
                        p.NameVi,
                    MinPrice = p.MinPrice,
                    MaxPrice = p.MaxPrice
                }).ToList();

                var propTypes = db.PropertyTypes
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.DisplayOrder)
                    .ToList();

                vm.PropertyTypes = propTypes.Select(t => new PropertyTypeDropDownItem
                {
                    Code = t.Code,
                    Name =
                        lang == "en" ? (t.NameEn ?? t.NameVi) :
                        lang == "zh" ? (t.NameZh ?? t.NameVi) :
                        t.NameVi
                }).ToList();

                var cityForBg = vm.CityId.HasValue
                    ? cities.FirstOrDefault(c => c.CityId == vm.CityId.Value)
                    : cities.FirstOrDefault();

                ViewBag.HeroBackground = cityForBg?.BackgroundUrl ?? "/Assets/Banner.jpg";
            }

            var userId = GetCurrentUserId();
            HashSet<int> favoriteIds = null;

            if (userId.HasValue)
            {
                var favList = await _favoriteService.GetFavoritesAsync(userId.Value, lang);
                favoriteIds = new HashSet<int>(favList.Select(f => f.PropertyId));
            }

            bool hasAnyFilter =
                !string.IsNullOrWhiteSpace(keyword) ||
                cityId.HasValue ||
                !string.IsNullOrWhiteSpace(priceRange) ||
                !string.IsNullOrWhiteSpace(propertyType) ||
                !string.IsNullOrWhiteSpace(transactionType);

            vm.HasSearch = hasAnyFilter;

            if (vm.HasSearch)
            {
                string listingType = string.IsNullOrWhiteSpace(transactionType) ? null : transactionType;

                var all = await _propertyService.SearchAsync(
                    lang, listingType, vm.CityId, vm.PriceRange, vm.PropertyType, vm.Keyword);

                vm.TotalItems = all.Count;
                vm.TotalPages = (int)Math.Ceiling(vm.TotalItems / (double)vm.PageSize);

                var pageData = all
                    .Skip((vm.Page - 1) * vm.PageSize)
                    .Take(vm.PageSize)
                    .ToList();

                vm.SearchResults = pageData.Select(x => ToHomeItem(x, favoriteIds)).ToList();
            }
            else
            {
                vm.TransactionType = null;

                // SearchAsync đã order featured trước rồi
                var rentList = await _propertyService.SearchAsync(lang, "rent", null, null, null, null);
                var saleList = await _propertyService.SearchAsync(lang, "sale", null, null, null, null);

                vm.FeaturedRent = rentList.Take(4).Select(x => ToHomeItem(x, favoriteIds)).ToList();
                vm.FeaturedSale = saleList.Take(4).Select(x => ToHomeItem(x, favoriteIds)).ToList();
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
            int page = 1)
        {
            const int PageSize = 16;

            var uiLang = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            var lang = uiLang.Length >= 2 ? uiLang.Substring(0, 2).ToLower() : "vi";

            var userId = GetCurrentUserId();
            HashSet<int> favoriteIds = null;

            if (userId.HasValue)
            {
                var favList = await _favoriteService.GetFavoritesAsync(userId.Value, lang);
                favoriteIds = new HashSet<int>(favList.Select(f => f.PropertyId));
            }
            ViewBag.FavoriteCount = favoriteIds?.Count ?? 0;
            string listingType = string.IsNullOrWhiteSpace(transactionType) ? null : transactionType;

            var all = await _propertyService.SearchAsync(
                lang, listingType, cityId, priceRange, propertyType, keyword);

            var pageData = all
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var list = pageData.Select(x => ToHomeItem(x, favoriteIds)).ToList();

            return PartialView("_PropertyCardList", list);
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
            var vi = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
            var s = price.ToString("#,0", vi) + " ";
            if ((listingType ?? "").ToLower() == "rent") s += "";
            return s;
        }
    }
}
