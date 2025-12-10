using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("cities", Schema = "public")]
    public class City
    {
        [Column("city_id")]
        public int CityId { get; set; }

        [Column("code")]
        public string Code { get; set; }

        [Column("name_vi")]
        public string NameVi { get; set; }

        [Column("name_en")]
        public string NameEn { get; set; }

        [Column("name_zh")]
        public string NameZh { get; set; }

        [Column("background_url")]
        public string BackgroundUrl { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("display_order")]
        public int? DisplayOrder { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
