using System;
using System.Collections.Generic;

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

        // --- Phân trang 
        public int Page { get; set; }       
        public int PageSize { get; set; }    
        public int TotalItems { get; set; }    
        public int TotalPages { get; set; }   
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
        public string PriceLabel { get; set; }

        public float Area { get; set; }
        public int? Bed { get; set; }
        public int? Bath { get; set; }

        public string ThumbnailUrl { get; set; }

        public string ListingType { get; set; }  // rent / sale
        public string PropertyType { get; set; }
        public int? CityId { get; set; }
        public bool IsFavorite { get; set; }
       
    }

    public class CityDropDownItem
    {
        public int CityId { get; set; }
        public string Name { get; set; }          // tên hiển thị
        public string BackgroundUrl { get; set; } // ảnh background cho banner
    }

    public class PriceFilterDropDownItem
    {
        // Chuỗi code dùng để gửi lên query: "0-10", "10-20", "20-999"...
        public string Code { get; set; }

        // Tên hiển thị theo ngôn ngữ
        public string Name { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }

    public class PropertyTypeDropDownItem
    {
        // Code: apartment / house / villa...
        public string Code { get; set; }

        // Tên hiển thị theo ngôn ngữ
        public string Name { get; set; }
    }
}
