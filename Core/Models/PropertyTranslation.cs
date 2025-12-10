using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("property_translations", Schema = "public")]
    public class PropertyTranslation
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("property_id")]
        public int PropertyId { get; set; }

        [Column("lang_code")]
        public string LangCode { get; set; }   // "vi", "en", "zh"...

        [Column("title")]
        public string Title { get; set; }

        [Column("display_title")]
        public string DisplayTitle { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("address_line")]
        public string AddressLine { get; set; }

        [Column("room_type")]
        public string RoomType { get; set; }

        [Column("orientation")]
        public string Orientation { get; set; }

        // FK về Property (logic thôi, không cần constraint trong DB)
        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }
    }
}
