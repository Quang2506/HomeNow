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

        // 'equirect' | 'multires'
        [Column("pano_type")]
        public string PanoType { get; set; }

        // equirect: url ảnh 2:1
        // multires: basePath folder tiles (VD: /Assets/VrTiles/1220/kitchen)
        [Column("panorama_url")]
        public string PanoramaUrl { get; set; }

        // optional: ảnh nhỏ để preview khi multires
        [Column("preview_url")]
        public string PreviewUrl { get; set; }

        // multires params
        [Column("tile_resolution")]
        public int TileResolution { get; set; } = 512;

        [Column("max_level")]
        public int MaxLevel { get; set; } = 5;

        [Column("cube_resolution")]
        public int CubeResolution { get; set; } = 4096;

        [Column("mr_path")]
        public string MrPath { get; set; } = "/%l/%s%y_%x";

        [Column("mr_fallback_path")]
        public string MrFallbackPath { get; set; } = "/fallback";

        [Column("mr_extension")]
        public string MrExtension { get; set; }/* = "jpg";*/

        // default view
        [Column("hfov")]
        public double Hfov { get; set; } = 110;

        [Column("pitch")]
        public double Pitch { get; set; } = 0;

        [Column("yaw")]
        public double Yaw { get; set; } = 0;

        [Column("is_default")]
        public bool IsDefault { get; set; }

        public virtual ICollection<VrHotspot> Hotspots { get; set; }
        public virtual ICollection<VrSceneTranslation> Translations { get; set; }
    }
}
