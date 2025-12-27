using System;
using System.Collections.Generic;
using Core.Helpers; // dùng MoneyText

namespace Core.Models
{
    public class HomeIndexViewModel
    {
        // --- Filter từ form search ---
        public string TransactionType { get; set; }
        public int? CityId { get; set; }
        public string PriceRange { get; set; }
        public string PropertyType { get; set; }
        public string Keyword { get; set; }

        // --- Kết quả tìm kiếm ---
        public bool HasSearch { get; set; }
        public List<PropertyListItemViewModel> SearchResults { get; set; }

        // --- Trang chủ (khi chưa search) ---
        public List<PropertyListItemViewModel> FeaturedRent { get; set; }
        public List<PropertyListItemViewModel> FeaturedSale { get; set; }

        // --- Dữ liệu cho combobox ---
        public List<CityDropDownItem> Cities { get; set; }
        public List<PriceFilterDropDownItem> PriceFilters { get; set; }
        public List<PropertyTypeDropDownItem> PropertyTypes { get; set; }

        // --- Phân trang ---
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public HomeIndexViewModel()
        {
            SearchResults = new List<PropertyListItemViewModel>();
            FeaturedRent = new List<PropertyListItemViewModel>();
            FeaturedSale = new List<PropertyListItemViewModel>();

            Cities = new List<CityDropDownItem>();
            PriceFilters = new List<PriceFilterDropDownItem>();
            PropertyTypes = new List<PropertyTypeDropDownItem>();
        }
    }

    /// <summary>
    /// Item hiển thị 1 căn nhà trên Home.
    /// </summary>
    public class PropertyListItemViewModel
    {
        public int PropertyId { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }

        public decimal Price { get; set; }

        // Giữ lại để không ảnh hưởng chỗ cũ đang dùng PriceLabel
        public string PriceLabel { get; set; }

        public float Area { get; set; }
        public int? Bed { get; set; }
        public int? Bath { get; set; }

        public string ThumbnailUrl { get; set; }

        public string ListingType { get; set; }  // rent / sale
        public string PropertyType { get; set; }
        public int? CityId { get; set; }
        public bool IsFavorite { get; set; }

        /// <summary>
        /// Computed property: hiển thị ngắn kiểu "25 triệu" / "2.5 tỷ"
        /// - Nếu rent => thêm "/tháng"
        /// </summary>
        public string PriceShort
        {
            get { return MoneyText.ToPriceShort(Price, ListingType); }
        }

    }

    public class CityDropDownItem
    {
        public int CityId { get; set; }
        public string Name { get; set; }         
        public string BackgroundUrl { get; set; } 
    }

    public class PriceFilterDropDownItem
    {
        
        public string Code { get; set; }

     
        public string Name { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }

    public class PropertyTypeDropDownItem
    {
        
        public string Code { get; set; }

       
        public string Name { get; set; }
    }
}
