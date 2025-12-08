using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("vr_hotspot", Schema = "public")]
    public class VrHotspot
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("scene_id")]
        public int SceneId { get; set; }

        [Column("pitch")]
        public double Pitch { get; set; }

        [Column("yaw")]
        public double Yaw { get; set; }

        [Column("text")]
        public string Text { get; set; }                // Chữ mặc định

        [Column("target_scene_key")]
        public string TargetSceneKey { get; set; }

        public virtual VrScene Scene { get; set; }

        public virtual ICollection<VrHotspotTranslation> Translations { get; set; }
    }
}
