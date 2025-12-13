namespace Core.ViewModels
{
    public class PropertyListViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public string AreaName { get; set; }
        public string CommunityName { get; set; }
        public string District { get; set; }
        public string RoomType { get; set; }
        public string Orientation { get; set; }

        public string CoverImageUrl { get; set; }
        public decimal? Price { get; set; }
        public string PriceUnit { get; set; }
        public decimal? AreaM2 { get; set; }

        public bool IsVrAvailable { get; set; }

        public bool IsFavorite { get; set; }
        public string ListingType { get; set; }

        // apartment / house / villa / office / studio ...
        public string PropertyType { get; set; }
    }
}
