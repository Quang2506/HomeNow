using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data
{
    /// <summary>
    /// Map to Postgres table: public.favorites
    /// status: 1 = liked, 0 = unliked (kept for history)
    /// </summary>
    [Table("favorites")]
    public class Favorite
    {
        [Key]
        [Column("favorite_id")]
        public int FavoriteId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("property_id")]
        public int PropertyId { get; set; }

        [Column("status")]
        public short Status { get; set; } = 1;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
