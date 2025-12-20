using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("properties", Schema = "public")]
    public class Property
    {
        [Key]
        [Column("property_id")]
        public int PropertyId { get; set; }

        [Column("owner_id")]
        public int? OwnerId { get; set; }

        [Column("city_id")]
        public int? CityId { get; set; }

        [Column("ward_id")]
        public int? WardId { get; set; }

        [Column("project_id")]
        public int? ProjectId { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("description")]
        public string Description { get; set; }

     
        [Column("listing_type")]
        public string ListingType { get; set; }

      
        [Column("property_type")]
        public string PropertyType { get; set; }

    
        [Column("price")]
        public decimal? Price { get; set; }

       
        [Column("area_sqm")]
        public float? AreaSqm { get; set; }

        [Column("bedroom_count")]
        public int? BedroomCount { get; set; }

        [Column("bathroom_count")]
        public int? BathroomCount { get; set; }

        [Column("living_room")]
        public int? LivingRoom { get; set; }

        [Column("kitchen")]
        public int? Kitchen { get; set; }

     
        [Column("address_line")]
        public string AddressLine { get; set; }

     
        [Column("status")]
        public string Status { get; set; }

        [Column("amenities")]
        public string Amenities { get; set; }

        [Column("is_featured")]
        public int? IsFeatured { get; set; }

        [Column("is_vr_available")]
        public bool? IsVrAvailable { get; set; }

        [Column("cover_image_url")]
        public string CoverImageUrl { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("created_uid")]
        public string CreatedUid { get; set; }

        [Column("updated_uid")]
        public string UpdatedUid { get; set; }

        
        public virtual ICollection<PropertyTranslation> Translations { get; set; }

    
        public virtual ICollection<VrScene> Scenes { get; set; }
    }
}
