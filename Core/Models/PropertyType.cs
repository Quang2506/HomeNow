using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("property_types", Schema = "public")]
    public class PropertyType
    {
        [Column("id")]
        public int Id { get; set; }

  
        [Column("code")]
        public string Code { get; set; }

        [Column("name_vi")]
        public string NameVi { get; set; }

        [Column("name_en")]
        public string NameEn { get; set; }

        [Column("name_zh")]
        public string NameZh { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; }
    }
}
