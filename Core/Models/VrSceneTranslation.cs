using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("vr_scene_translations", Schema = "public")]
    public class VrSceneTranslation
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("scene_id")]
        public int SceneId { get; set; }

        [Column("lang_code")]
        public string LangCode { get; set; }        // vi/en/zh

        [Column("title")]
        public string Title { get; set; }

        public virtual VrScene Scene { get; set; }
    }
}
