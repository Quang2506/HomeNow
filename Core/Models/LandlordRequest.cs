using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("landlord_requests", Schema = "public")]
    public class LandlordRequest
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("owner_name")]
        public string OwnerName { get; set; }

        [Column("phone")]
        public string Phone { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("address")]
        public string Address { get; set; }

        [Column("property_type")]
        public string PropertyType { get; set; }

        [Column("package_type")]
        public string PackageType { get; set; }

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
