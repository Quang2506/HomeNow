using Core.Models;
using Core.ViewModels;
using Data;
using Services.Interfaces;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class PropertyService : IPropertyService
    {
        public async Task<List<PropertyListViewModel>> SearchAsync(
            string langCode,
            string listingType,
            int? cityId,
            string priceRange,
            string propertyType,
            string keyword)
        {
            using (var db = new AppDbContext())
            {
                var query =
                    from p in db.Properties
                    join tr in db.PropertyTranslations
                        on new { Id = p.PropertyId, Lang = langCode }
                        equals new { Id = tr.PropertyId, Lang = tr.LangCode }
                        into gj
                    from tr in gj.DefaultIfEmpty()
                    where p.Status == "published"
                    select new { p, tr };

                if (!string.IsNullOrWhiteSpace(listingType))
                    query = query.Where(x => x.p.ListingType == listingType);

                if (cityId.HasValue)
                    query = query.Where(x => x.p.CityId == cityId.Value);

                if (!string.IsNullOrWhiteSpace(propertyType))
                    query = query.Where(x => x.p.PropertyType == propertyType);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLower();

                    query = query.Where(x =>
                        (
                            (((x.tr != null ? x.tr.Title : null) ?? x.p.Title) ?? "")
                            .ToLower().Contains(kw)
                        )
                        ||
                        (
                            (((x.tr != null ? x.tr.AddressLine : null) ?? x.p.AddressLine) ?? "")
                            .ToLower().Contains(kw)
                        )
                    );
                }

                decimal? minPrice = null;
                decimal? maxPrice = null;

                if (!string.IsNullOrEmpty(priceRange))
                {
                    var pf = db.PriceFilters
                               .Where(f => f.IsActive && f.Code == priceRange)
                               .Select(f => new { f.MinPrice, f.MaxPrice })
                               .FirstOrDefault();

                    if (pf != null)
                    {
                        minPrice = pf.MinPrice;
                        maxPrice = pf.MaxPrice;
                    }
                }

                if (minPrice.HasValue) query = query.Where(x => x.p.Price >= minPrice.Value);
                if (maxPrice.HasValue) query = query.Where(x => x.p.Price <= maxPrice.Value);

                var raw = await query
                    .OrderByDescending(x => (x.p.IsFeatured ?? 0))
                    .ThenByDescending(x => (x.p.CreatedAt ?? System.DateTime.MinValue))
                    .ThenByDescending(x => x.p.PropertyId)
                    .ToListAsync();

                var list = raw.Select(x =>
                {
                    var p = x.p;
                    var tr = x.tr;

                    return new PropertyListViewModel
                    {
                        Id = p.PropertyId,

                        Title = tr != null && !string.IsNullOrEmpty(tr.DisplayTitle)
                                    ? tr.DisplayTitle
                                    : (tr != null && !string.IsNullOrEmpty(tr.Title)
                                        ? tr.Title
                                        : p.Title),

                        Address = tr != null && !string.IsNullOrEmpty(tr.AddressLine)
                                    ? tr.AddressLine
                                    : p.AddressLine,

                        AreaName = null,
                        CommunityName = null,
                        District = null,

                        RoomType = tr?.RoomType,
                        Orientation = tr?.Orientation,

                        CoverImageUrl = p.CoverImageUrl,
                        Price = p.Price,
                        PriceUnit = null,

             
                        AreaM2 = p.AreaSqm.HasValue ? (decimal?)p.AreaSqm.Value : null,

                
                        BedroomCount = p.BedroomCount,
                        BathroomCount = p.BathroomCount,
                        AreaSqm = p.AreaSqm,

                    
                        ListingType = p.ListingType,
                        PropertyType = p.PropertyType,

                        IsVrAvailable = p.IsVrAvailable ?? false,
                        IsFavorite = false
                    };
                }).ToList();

                return list;
            }
        }

        public async Task<Property> GetByIdAsync(int id)
        {
            using (var db = new AppDbContext())
            {
                return await db.Properties
                    .Include(p => p.Translations)
                    .Include(p => p.Scenes)
                    .FirstOrDefaultAsync(p => p.PropertyId == id);
            }
        }
    }
}
