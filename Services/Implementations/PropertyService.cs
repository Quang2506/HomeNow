using Core.Models;
using Core.ViewModels;
using Data;
using Services.Interfaces;
using System;
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
                // JOIN properties + property_translations theo lang
                var query =
                    from p in db.Properties
                    join tr in db.PropertyTranslations
                        on new { Id = p.PropertyId, Lang = langCode }
                        equals new { Id = tr.PropertyId, Lang = tr.LangCode }
                        into gj
                    from tr in gj.DefaultIfEmpty()
                    where p.Status == "published"
                    select new { p, tr };

                // --- Thuê / Mua ---
                if (!string.IsNullOrWhiteSpace(listingType))
                    query = query.Where(x => x.p.ListingType == listingType);

                // --- Thành phố ---
                if (cityId.HasValue)
                    query = query.Where(x => x.p.CityId == cityId.Value);

                // --- Loại nhà ---
                if (!string.IsNullOrWhiteSpace(propertyType))
                    query = query.Where(x => x.p.PropertyType == propertyType);

                // --- Từ khóa: tìm trong title + address_line ---
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    query = query.Where(x =>
                        ((x.tr.Title ?? x.p.Title).Contains(keyword)) ||
                        ((x.tr.AddressLine ?? x.p.AddressLine).Contains(keyword)));
                }

                // --- Khoảng giá ---
                decimal? minPrice = null, maxPrice = null;
                if (!string.IsNullOrEmpty(priceRange))
                {
                    var parts = priceRange.Split('-');
                    if (parts.Length == 2)
                    {
                        if (decimal.TryParse(parts[0], out var min)) minPrice = min;
                        if (decimal.TryParse(parts[1], out var max)) maxPrice = max;
                    }
                }
                if (minPrice.HasValue)
                    query = query.Where(x => x.p.Price >= minPrice.Value);
                if (maxPrice.HasValue)
                    query = query.Where(x => x.p.Price <= maxPrice.Value);

                // 1) Lấy dữ liệu thô từ DB
                var raw = await query
                    .OrderByDescending(x => x.p.CreatedAt)
                    .ToListAsync();

                // 2) Map sang ViewModel trong C#
                var list = raw
                    .Select(x =>
                    {
                        var p = x.p;
                        var tr = x.tr;

                        return new PropertyListViewModel
                        {
                            Id = p.PropertyId,

                            // ưu tiên bản dịch
                            Title = tr?.DisplayTitle ?? tr?.Title ?? p.Title,
                            Address = tr?.AddressLine ?? p.AddressLine,

                            RoomType = tr?.RoomType,
                            Orientation = tr?.Orientation,

                            // hiện chưa có cột trong DB → để null
                            AreaName = null,
                            CommunityName = null,
                            District = null,

                            CoverImageUrl = p.CoverImageUrl,
                            Price = p.Price,
                            PriceUnit = null,
                            AreaM2 = p.AreaSqm.HasValue
                                ? (decimal?)p.AreaSqm.Value
                                : null,

                            IsVrAvailable = p.IsVrAvailable ?? false,
                            IsFavorite = false
                        };
                    })
                    .ToList();

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
