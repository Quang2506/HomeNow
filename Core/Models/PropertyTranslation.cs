using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("property_translations", Schema = "public")]
    public class PropertyTranslation
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("property_id")]
        public int PropertyId { get; set; }

        [Column("lang_code")]
        public string LangCode { get; set; }        // vi, en, zh

        [Column("title")]
        public string Title { get; set; }

        [Column("display_title")]
        public string DisplayTitle { get; set; }

        [Column("address")]
        public string Address { get; set; }

        [Column("district")]
        public string District { get; set; }

        [Column("city")]
        public string City { get; set; }

        [Column("area_name")]
        public string AreaName { get; set; }

        [Column("community_name")]
        public string CommunityName { get; set; }

        [Column("room_type")]
        public string RoomType { get; set; }

        [Column("orientation")]
        public string Orientation { get; set; }

        public virtual Property Property { get; set; }
    }
}
