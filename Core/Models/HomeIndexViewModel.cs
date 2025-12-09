using System.Collections.Generic;

namespace Core.Models
{
    public class HomeIndexViewModel
    {
        // filter trên thanh search
        public string TransactionType { get; set; } = "rent"; // rent / buy
        public string Keyword { get; set; }
        public string PriceRange { get; set; }
        public string PropertyType { get; set; }

        // cờ xác định có đang ở chế độ search không
        public bool HasSearch { get; set; }

        // dữ liệu hiển thị
        public List<PropertyCard> FeaturedRent { get; set; } = new List<PropertyCard>();
        public List<PropertyCard> FeaturedSale { get; set; } = new List<PropertyCard>();
        public List<PropertyCard> SearchResults { get; set; } = new List<PropertyCard>();
    }

    // card đơn giản để render ra UI
    public class PropertyCard
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public string ThumbnailUrl { get; set; }

        public decimal Price { get; set; }
        public string PriceLabel { get; set; }   // "18 triệu/tháng", "6.5 tỷ"…
        public int Bed { get; set; }
        public int Bath { get; set; }
        public int Area { get; set; }            // m2
    }
}
