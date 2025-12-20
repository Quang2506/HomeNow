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
            
            var paged = await SearchPagedAsync(langCode, listingType, cityId, priceRange, propertyType, keyword, 1, 1000000);
            return paged.Items ?? new List<PropertyListViewModel>();
        }

        public async Task<PagedResult<PropertyListViewModel>> SearchPagedAsync(
        string langCode,
        string listingType,
        int? cityId,
        string priceRange,
        string propertyType,
        string keyword,
        int page,
        int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 16;

            using (var db = new AppDbContext())
            {
                var query =
                    from p in db.Properties.AsNoTracking()
                    join tr in db.PropertyTranslations.AsNoTracking()
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
                        (((x.tr != null ? x.tr.Title : null) ?? x.p.Title) ?? "").ToLower().Contains(kw) ||
                        (((x.tr != null ? x.tr.AddressLine : null) ?? x.p.AddressLine) ?? "").ToLower().Contains(kw)
                    );
                }

                if (!string.IsNullOrEmpty(priceRange))
                {
                    var pf = await db.PriceFilters.AsNoTracking()
                        .Where(f => f.IsActive && f.Code == priceRange)
                        .Select(f => new { f.MinPrice, f.MaxPrice })
                        .FirstOrDefaultAsync();

                    if (pf != null)
                    {
                        if (pf.MinPrice.HasValue) query = query.Where(x => x.p.Price >= pf.MinPrice.Value);
                        if (pf.MaxPrice.HasValue) query = query.Where(x => x.p.Price <= pf.MaxPrice.Value);
                    }
                }

                var total = await query.CountAsync();

              
                var pageRows = await query
                    .OrderByDescending(x => x.p.IsFeatured)          // nếu IsFeatured là bool/int => OK
                    .ThenByDescending(x => x.p.CreatedAt)            // tránh DateTime.MinValue trong query
                    .ThenByDescending(x => x.p.PropertyId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                
                var items = pageRows.Select(x =>
                {
                    var p = x.p;
                    var tr = x.tr;

                    return new PropertyListViewModel
                    {
                        Id = p.PropertyId,
                        Title = (tr != null && !string.IsNullOrEmpty(tr.DisplayTitle)) ? tr.DisplayTitle
                              : (tr != null && !string.IsNullOrEmpty(tr.Title) ? tr.Title : p.Title),

                        Address = (tr != null && !string.IsNullOrEmpty(tr.AddressLine)) ? tr.AddressLine : p.AddressLine,

                        CoverImageUrl = p.CoverImageUrl,
                        Price = p.Price,

                     
                        AreaM2 = p.AreaSqm.HasValue ? (decimal?)p.AreaSqm.Value : null,
                        AreaSqm = p.AreaSqm,
                        BedroomCount = p.BedroomCount,
                        BathroomCount = p.BathroomCount,

                        ListingType = p.ListingType,
                        PropertyType = p.PropertyType,

                        IsVrAvailable = p.IsVrAvailable ?? false,
                        IsFavorite = false
                    };
                }).ToList();

                return new PagedResult<PropertyListViewModel>
                {
                    TotalItems = total,
                    Items = items
                };
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
