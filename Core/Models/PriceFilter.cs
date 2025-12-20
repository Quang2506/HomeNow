using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("price_filters", Schema = "public")]
    public class PriceFilter
    {
        [Column("id")]
        public int Id { get; set; }

     
        [Column("code")]
        public string Code { get; set; }

        // 'rent' / 'sale' hoặc NULL (áp dụng cho cả 2)
        [NotMapped]
        public string ListingType { get; set; }

        [Column("name_vi")]
        public string NameVi { get; set; }

        [Column("name_en")]
        public string NameEn { get; set; }

        [Column("name_zh")]
        public string NameZh { get; set; }

        [Column("min_price")]
        public decimal? MinPrice { get; set; }

        [Column("max_price")]
        public decimal? MaxPrice { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
