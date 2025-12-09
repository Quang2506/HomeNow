using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Core.Models;



namespace HomeNow.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(string transactionType, string keyword, string priceRange, string propertyType)
        {
            var vm = new HomeIndexViewModel
            {
                TransactionType = string.IsNullOrEmpty(transactionType) ? "rent" : transactionType,
                Keyword = keyword,
                PriceRange = priceRange,
                PropertyType = propertyType
            };

            // ---- DUMMY DATA: sau này bạn thay bằng query DB / PropertyService ----
            var allRent = new List<PropertyCard>
    {
        new PropertyCard {
            Id = 1,
            Title = "Căn hộ 2PN full nội thất",
            Address = "Bình Thạnh, TP.HCM",
            ThumbnailUrl = "/Assets/house_rent_1.jpg",
            Price = 18, PriceLabel = "18 triệu/tháng",
            Bed = 2, Bath = 2, Area = 80
        },
        new PropertyCard {
            Id = 2,
            Title = "Studio view sông",
            Address = "Quận 2, TP.HCM",
            ThumbnailUrl = "/Assets/house_rent_2.jpg",
            Price = 10, PriceLabel = "10 triệu/tháng",
            Bed = 1, Bath = 1, Area = 35
        }
    };

            var allSale = new List<PropertyCard>
    {
        new PropertyCard {
            Id = 3,
            Title = "Nhà phố 3 tầng, sân thượng",
            Address = "Quận 7, TP.HCM",
            ThumbnailUrl = "/Assets/house_sale_1.jpg",
            Price = 6.5m, PriceLabel = "6.5 tỷ",
            Bed = 4, Bath = 3, Area = 120
        },
        new PropertyCard {
            Id = 4,
            Title = "Căn hộ 3PN cao cấp",
            Address = "Thủ Đức, TP.HCM",
            ThumbnailUrl = "/Assets/house_sale_2.jpg",
            Price = 5.2m, PriceLabel = "5.2 tỷ",
            Bed = 3, Bath = 2, Area = 95
        }
    };
            // -----------------------------------------------------

            var hasFilter = !string.IsNullOrWhiteSpace(keyword)
                            || !string.IsNullOrWhiteSpace(priceRange)
                            || !string.IsNullOrWhiteSpace(propertyType);

            if (!hasFilter)
            {
                vm.HasSearch = false;
                vm.FeaturedRent = allRent;
                vm.FeaturedSale = allSale;
            }
            else
            {
                vm.HasSearch = true;
                var list = allRent.Concat(allSale).AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var lower = keyword.ToLower();
                    list = list.Where(x =>
                        x.Title.ToLower().Contains(lower) ||
                        x.Address.ToLower().Contains(lower));
                }

                if (!string.IsNullOrWhiteSpace(priceRange))
                {
                    var parts = priceRange.Split('-');
                    if (parts.Length == 2
                        && decimal.TryParse(parts[0], out var min)
                        && decimal.TryParse(parts[1], out var max))
                    {
                        list = list.Where(x => x.Price >= min && x.Price <= max);
                    }
                }

                // propertyType (demo, chưa filter thật)
                vm.SearchResults = list.ToList();
            }

            return View(vm);
        }

    }
}