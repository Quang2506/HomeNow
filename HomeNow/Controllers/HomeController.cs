using System;
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
    public class HomeController : Controller
    {
        private readonly IPropertyService _propertyService;

        public HomeController()
        {
            _propertyService = new PropertyService();
        }

        public async Task<ActionResult> Index(
            string transactionType,
            int? cityId,
            string priceRange,
            string propertyType,
            string keyword,
            int page = 1)
        {
            const int PageSize = 20;

            var uiLang = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            var lang = uiLang.Length >= 2 ? uiLang.Substring(0, 2).ToLower() : "vi";

            var vm = new HomeIndexViewModel
            {
                TransactionType = transactionType, // không default = "rent" nữa
                CityId = cityId,
                PriceRange = priceRange,
                PropertyType = propertyType,
                Keyword = keyword,
                Page = page,
                PageSize = PageSize
            };

            // 2) Nạp dữ liệu cho combobox
            using (var db = new AppDbContext())
            {
                // Thành phố
                var cities = db.Cities
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToList();

                vm.Cities = cities
                    .Select(c => new CityDropDownItem
                    {
                        CityId = c.CityId,
                        Name =
                            lang == "en" ? (c.NameEn ?? c.NameVi) :
                            lang == "zh" ? (c.NameZh ?? c.NameVi) :
                            c.NameVi,
                        BackgroundUrl = c.BackgroundUrl
                    })
                    .ToList();

                // Mốc giá
                var priceFilters = db.PriceFilters
                 .Where(p => p.IsActive)
                 .OrderBy(p => p.DisplayOrder)
                 .ToList();

                vm.PriceFilters = priceFilters
                    .Select(p => new PriceFilterDropDownItem
                    {
                        Code = p.Code,
                        Name = lang == "en" ? (p.NameEn ?? p.NameVi) :
                                   lang == "zh" ? (p.NameZh ?? p.NameVi) :
                                   p.NameVi,
                        MinPrice = p.MinPrice,
                        MaxPrice = p.MaxPrice
                    })
                    .ToList();


                // Loại nhà
                var propTypes = db.PropertyTypes
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.DisplayOrder)
                    .ToList();

                vm.PropertyTypes = propTypes
                    .Select(t => new PropertyTypeDropDownItem
                    {
                        Code = t.Code,
                        Name =
                            lang == "en" ? (t.NameEn ?? t.NameVi) :
                            lang == "zh" ? (t.NameZh ?? t.NameVi) :
                            t.NameVi
                    })
                    .ToList();

                // Background theo city (home)
                var cityForBg = vm.CityId.HasValue
                    ? cities.FirstOrDefault(c => c.CityId == vm.CityId.Value)
                    : cities.FirstOrDefault();

                ViewBag.HeroBackground = cityForBg?.BackgroundUrl ?? "/Assets/Banner.jpg";
            }

            // 3) Xác định có đang search hay không
            bool hasAnyFilter =
                !string.IsNullOrWhiteSpace(keyword) ||
                cityId.HasValue ||
                !string.IsNullOrWhiteSpace(priceRange) ||
                !string.IsNullOrWhiteSpace(propertyType) ||
                !string.IsNullOrWhiteSpace(transactionType);   // <<< THÊM DÒNG NÀY

            vm.HasSearch = hasAnyFilter;

            if (vm.HasSearch)
            {
                // Nếu user không chọn Thuê/Mua thì không filter theo listingType
                string listingType = string.IsNullOrWhiteSpace(transactionType)
                    ? null
                    : transactionType;

                var all = await _propertyService.SearchAsync(
                    lang,
                    listingType,      // rent / sale / null (tất cả)
                    vm.CityId,
                    vm.PriceRange,
                    vm.PropertyType,
                    vm.Keyword);

                vm.TotalItems = all.Count;
                vm.TotalPages = (int)Math.Ceiling(vm.TotalItems / (double)vm.PageSize);

                var pageData = all
                    .Skip((vm.Page - 1) * vm.PageSize)
                    .Take(vm.PageSize)
                    .ToList();

                vm.SearchResults = pageData.Select(ToHomeItem).ToList();
            }
            else
            {
                // Trang home nổi bật: không chọn Thuê/Mua → TransactionType = null để view không highlight nút
                vm.TransactionType = null;

                var rentList = await _propertyService.SearchAsync(
                    lang, "rent", null, null, null, null);
                var saleList = await _propertyService.SearchAsync(
                    lang, "sale", null, null, null, null);

                vm.FeaturedRent = rentList
                    .Take(4)
                    .Select(ToHomeItem)
                    .ToList();

                vm.FeaturedSale = saleList
                    .Take(4)
                    .Select(ToHomeItem)
                    .ToList();
            }

            return View(vm);
        }

        private PropertyListItemViewModel ToHomeItem(PropertyListViewModel x)
        {
            return new PropertyListItemViewModel
            {
                PropertyId = x.Id,
                Title = x.Title,
                Address = x.Address,
                Price = x.Price ?? 0,
                PriceLabel = x.Price.HasValue ? $"{x.Price:N0}" : "",
                Area = (float)(x.AreaM2 ?? 0),
                Bed = null,
                Bath = null,
                ThumbnailUrl = x.CoverImageUrl
            };
        }
    }
}
