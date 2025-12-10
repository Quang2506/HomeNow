using Core.Models;
using Core.ViewModels;
using Data;
using Services.Implementations;
using Services.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

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
            string keyword)
        {
            var vm = new HomeIndexViewModel
            {
                TransactionType = string.IsNullOrEmpty(transactionType) ? "rent" : transactionType,
                CityId = cityId,
                PriceRange = priceRange,
                PropertyType = propertyType,
                Keyword = keyword,
            };

            // --------- Lấy danh sách city từ DB ----------
            using (var db = new AppDbContext())
            {
                var uiLang = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
                var lang = uiLang.Length >= 2 ? uiLang.Substring(0, 2).ToLower() : "vi";

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
            }

            // --------- Gọi search hoặc featured như bạn đang làm ----------
            var uiLang2 = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            var langCode = uiLang2.Length >= 2 ? uiLang2.Substring(0, 2).ToLower() : "vi";

            var hasSearch = !string.IsNullOrWhiteSpace(keyword)
                            || cityId.HasValue
                            || !string.IsNullOrWhiteSpace(priceRange)
                            || !string.IsNullOrWhiteSpace(propertyType);

            vm.HasSearch = hasSearch;

            if (hasSearch)
            {
                var list = await _propertyService.SearchAsync(
                    langCode,
                    vm.TransactionType,
                    vm.CityId,
                    vm.PriceRange,
                    vm.PropertyType,
                    vm.Keyword);

                vm.SearchResults = list.Select(ToHomeItem).ToList();
            }
            else
            {
                var rentList = await _propertyService.SearchAsync(
                    langCode, "rent", null, null, null, null);
                var saleList = await _propertyService.SearchAsync(
                    langCode, "sale", null, null, null, null);

                vm.FeaturedRent = rentList
                    .Take(6)
                    .Select(ToHomeItem)
                    .ToList();

                vm.FeaturedSale = saleList
                    .Take(6)
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
