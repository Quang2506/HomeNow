using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("vr_scene", Schema = "public")]
    public class VrScene
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("property_id")]
        public int PropertyId { get; set; }

        [Column("scene_key")]
        public string SceneKey { get; set; }        

        [Column("title")]
        public string Title { get; set; }           

        [Column("panorama_url")]
        public string PanoramaUrl { get; set; }

        [Column("hfov")]
        public double Hfov { get; set; }

        [Column("pitch")]
        public double Pitch { get; set; }

        [Column("yaw")]
        public double Yaw { get; set; }

        [Column("is_default")]
        public bool IsDefault { get; set; }

        public virtual ICollection<VrHotspot> Hotspots { get; set; }

        public virtual ICollection<VrSceneTranslation> Translations { get; set; }
    }
}
