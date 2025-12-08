using Core.Models;
using System.Data.Entity;

namespace Data   // <=== PHẢI LÀ 'Data'
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base("HomeNowConnection")
        {
        }

        // DbSet cũ của bạn
        public DbSet<LandlordRequest> LandlordRequests { get; set; }
        public DbSet<ViewingRequest> ViewingRequests { get; set; }

        // DbSet mới thêm
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyTranslation> PropertyTranslations { get; set; }
        public DbSet<VrScene> VrScenes { get; set; }
        public DbSet<VrSceneTranslation> VrSceneTranslations { get; set; }
        public DbSet<VrHotspot> VrHotspots { get; set; }
        public DbSet<VrHotspotTranslation> VrHotspotTranslations { get; set; }
    }
}
