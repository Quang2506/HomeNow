using System.Collections.Generic;

namespace Core.ViewModels
{
    public class PropertyDetailViewModel
    {
        public int PropertyId { get; set; }
        public string Title { get; set; }
        public string AddressLine { get; set; }
        public string CityName { get; set; }

        public string ListingType { get; set; }
        public string PropertyType { get; set; }

        public decimal Price { get; set; }
        public string PriceLabel { get; set; }

        public float Area { get; set; }
        public int? BedroomCount { get; set; }
        public int? BathroomCount { get; set; }

        public bool IsVrAvailable { get; set; }
        public string CoverImageUrl { get; set; }

        public string Description { get; set; }
        public string MapEmbedUrl { get; set; }
        public string MapClickUrl { get; set; }

        // ✅ ADD
        public bool IsFavorite { get; set; }

        public List<Core.Models.PropertyListItemViewModel> Similar { get; set; } = new List<Core.Models.PropertyListItemViewModel>();
        public List<string> Amenities { get; set; } = new List<string>();
    }
}
