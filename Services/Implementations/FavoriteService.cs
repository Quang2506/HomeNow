using Core.Models;   // PropertyListItemViewModel
using Data;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class FavoriteService : IFavoriteService
    {
        public async Task<List<PropertyListItemViewModel>> GetFavoritesAsync(int userId, string langCode)
        {
            using (var db = new AppDbContext())
            {
                langCode = (langCode ?? "vi").Substring(0, 2).ToLower();

                var raw = await
                    (from f in db.UserFavoriteProperties
                     join p in db.Properties on f.PropertyId equals p.PropertyId
                     where f.UserId == userId && p.Status == "published"
                     orderby f.CreatedAt descending
                     select new
                     {
                         Property = p,
                         Translation = p.Translations.FirstOrDefault(t => t.LangCode == langCode)
                     })
                    .ToListAsync();

                var list = raw.Select(x =>
                {
                    var p = x.Property;
                    var tr = x.Translation;

                    return new PropertyListItemViewModel
                    {
                        PropertyId = p.PropertyId,
                        Title = tr != null ? (tr.DisplayTitle ?? tr.Title) : p.Title,
                        Address = tr != null ? tr.AddressLine : p.AddressLine,

                        Price = p.Price ?? 0m,
                        PriceLabel = p.Price.HasValue ? p.Price.Value.ToString("N0") : "",

                        Area = (float)(p.AreaSqm ?? 0),
                        Bed = p.BedroomCount,
                        Bath = p.BathroomCount,
                        ThumbnailUrl = p.CoverImageUrl,
                        ListingType = p.ListingType,
                        PropertyType = p.PropertyType,
                        IsFavorite = true
                    };
                }).ToList();

                return list;
            }
        }

      
        public async Task<int[]> GetFavoriteIdsAsync(int userId)
        {
            using (var db = new AppDbContext())
            {
                var ids = await
                    (from f in db.UserFavoriteProperties
                     join p in db.Properties on f.PropertyId equals p.PropertyId
                     where f.UserId == userId && p.Status == "published"
                     select f.PropertyId)
                    .Distinct()
                    .ToArrayAsync();

                return ids ?? new int[0];
            }
        }

        
        public async Task<FavoriteToggleResult> ToggleFavoriteWithSummaryAsync(int userId, int propertyId, string langCode)
        {
            using (var db = new AppDbContext())
            {
                var fav = await db.UserFavoriteProperties
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.PropertyId == propertyId);

                bool isFavorite;

                if (fav == null)
                {
                    fav = new UserFavoriteProperty
                    {
                        UserId = userId,
                        PropertyId = propertyId,
                        CreatedAt = DateTime.Now
                    };

                    db.UserFavoriteProperties.Add(fav);
                    isFavorite = true;
                }
                else
                {
                    db.UserFavoriteProperties.Remove(fav);
                    isFavorite = false;
                }

                await db.SaveChangesAsync();

                //summary (published)
                var ids = await
                    (from f in db.UserFavoriteProperties
                     join p in db.Properties on f.PropertyId equals p.PropertyId
                     where f.UserId == userId && p.Status == "published"
                     select f.PropertyId)
                    .Distinct()
                    .ToArrayAsync();

                ids = ids ?? new int[0];

                return new FavoriteToggleResult
                {
                    IsFavorite = isFavorite,
                    FavoriteIds = ids,
                    FavoriteCount = ids.Length
                };
            }
        }

        //
        public async Task<bool> ToggleFavoriteAsync(int userId, int propertyId)
        {
            using (var db = new AppDbContext())
            {
                var fav = await db.UserFavoriteProperties
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.PropertyId == propertyId);

                bool isFavorite;

                if (fav == null)
                {
                    fav = new UserFavoriteProperty
                    {
                        UserId = userId,
                        PropertyId = propertyId,
                        CreatedAt = DateTime.Now
                    };

                    db.UserFavoriteProperties.Add(fav);
                    isFavorite = true;
                }
                else
                {
                    db.UserFavoriteProperties.Remove(fav);
                    isFavorite = false;
                }

                await db.SaveChangesAsync();
                return isFavorite;
            }
        }
    }
}
