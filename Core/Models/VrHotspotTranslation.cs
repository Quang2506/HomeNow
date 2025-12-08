using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("vr_hotspot_translations", Schema = "public")]
    public class VrHotspotTranslation
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("hotspot_id")]
        public int HotspotId { get; set; }

        [Column("lang_code")]
        public string LangCode { get; set; }      // vi/en/zh

        [Column("text")]
        public string Text { get; set; }

        public virtual VrHotspot Hotspot { get; set; }
    }
}
