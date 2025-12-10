using Core.Models;
using System.Data.Entity;

namespace Data   
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base("HomeNowConnection"){ }

     
        public DbSet<LandlordRequest> LandlordRequests { get; set; }
        public DbSet<ViewingRequest> ViewingRequests { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyTranslation> PropertyTranslations { get; set; }
        public DbSet<VrScene> VrScenes { get; set; }
        public DbSet<VrSceneTranslation> VrSceneTranslations { get; set; }
        public DbSet<VrHotspot> VrHotspots { get; set; }
        public DbSet<VrHotspotTranslation> VrHotspotTranslations { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<UserFavoriteProperty> UserFavoriteProperties { get; set; }
        public DbSet<City> Cities { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //  default schema là public thay vì dbo
            modelBuilder.HasDefaultSchema("public");

            base.OnModelCreating(modelBuilder);
        }
    }
}
