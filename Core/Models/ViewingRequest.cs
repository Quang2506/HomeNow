using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("viewing_requests", Schema = "public")]
    public class ViewingRequest
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("property_id")]
        public int? PropertyId { get; set; }

        [Column("customer_name")]
        public string CustomerName { get; set; }

        [Column("phone")]
        public string Phone { get; set; }

        [Column("preferred_time")]
        public DateTime? PreferredTime { get; set; }

        [Column("note")]
        public string Note { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
