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
        public string Text { get; set; }

        [Column("target_scene_key")]
        public string TargetSceneKey { get; set; }

        // NEW: view ở scene đích để chuyển “tự nhiên”
        [Column("target_pitch")]
        public double? TargetPitch { get; set; }

        [Column("target_yaw")]
        public double? TargetYaw { get; set; }

        [Column("target_hfov")]
        public double? TargetHfov { get; set; }

        public virtual VrScene Scene { get; set; }
        public virtual ICollection<VrHotspotTranslation> Translations { get; set; }
    }
}
