using System.Collections.Generic;

namespace Core.Models
{
    public class HomeIndexViewModel
    {
        // --- Filter ---
        public string TransactionType { get; set; }
        public int? CityId { get; set; }          // <== đã có
        public string PriceRange { get; set; }
        public string PropertyType { get; set; }
        public string Keyword { get; set; }

        // --- Kết quả tìm kiếm ---
        public bool HasSearch { get; set; }
        public List<PropertyListItemViewModel> SearchResults { get; set; }

        // --- Trang chủ ---
        public List<PropertyListItemViewModel> FeaturedRent { get; set; }
        public List<PropertyListItemViewModel> FeaturedSale { get; set; }

        // ====== NEW: danh sách city cho combobox ======
        public List<CityDropDownItem> Cities { get; set; }
    }

    public class PropertyListItemViewModel
    {
        public int PropertyId { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }



        public decimal Price { get; set; }
        public string PriceLabel { get; set; }



        public float Area { get; set; }
        public int? Bed { get; set; }
        public int? Bath { get; set; }



        public string ThumbnailUrl { get; set; }
        public string ListingType { get; set; }       // rent / sale
        public string PropertyType { get; set; }
        public int? CityId { get; set; }
    }

    public class CityDropDownItem
    {
        public int CityId { get; set; }
        public string Name { get; set; }          // tên hiển thị theo ngôn ngữ hiện tại
        public string BackgroundUrl { get; set; } // link ảnh background
    }
}
