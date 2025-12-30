using Core.Models;   // Favorite, Property, PropertyTranslation
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
        private static string NormalizeLang(string langCode)
        {
            if (string.IsNullOrWhiteSpace(langCode)) return "vi";
            langCode = langCode.Trim().ToLower();
            return langCode.Length >= 2 ? langCode.Substring(0, 2) : "vi";
        }

        /// <summary>
        /// Chỉ tính favorites cho property published (đúng theo UI).
        /// </summary>
        public async Task<List<PropertyListItemViewModel>> GetFavoritesAsync(int userId, string langCode)
        {
            using (var db = new AppDbContext())
            {
                langCode = NormalizeLang(langCode);

                var raw = await
                    (from f in db.Favorites.AsNoTracking()
                     join p in db.Properties.AsNoTracking() on f.PropertyId equals p.PropertyId
                     join tr in db.PropertyTranslations.AsNoTracking().Where(t => t.LangCode == langCode)
                        on p.PropertyId equals tr.PropertyId into gj
                     from tr in gj.DefaultIfEmpty()
                     where f.UserId == userId
                        && f.Status == 1
                        && p.Status == "published"
                     orderby f.CreatedAt descending
                     select new
                     {
                         Property = p,
                         Translation = tr
                     })
                    .ToListAsync()
                    .ConfigureAwait(false);

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

        /// <summary>
        /// Chỉ trả id của property published + status=1
        /// </summary>
        public async Task<int[]> GetFavoriteIdsAsync(int userId)
        {
            using (var db = new AppDbContext())
            {
                var ids = await
                    (from f in db.Favorites.AsNoTracking()
                     join p in db.Properties.AsNoTracking() on f.PropertyId equals p.PropertyId
                     where f.UserId == userId
                        && f.Status == 1
                        && p.Status == "published"
                     select f.PropertyId)
                    .Distinct()
                    .ToArrayAsync()
                    .ConfigureAwait(false);

                return ids ?? new int[0];
            }
        }

        /// <summary>
        /// Toggle + trả summary (ids + count). Có dọn trùng và chỉ cho published.
        /// </summary>
        public async Task<FavoriteToggleResult> ToggleFavoriteWithSummaryAsync(int userId, int propertyId, string langCode)
        {
            using (var db = new AppDbContext())
            {
                // ✅ chỉ cho toggle nếu property published (tránh UI ảo rồi revert)
                var isPublished = await db.Properties.AsNoTracking()
                    .AnyAsync(p => p.PropertyId == propertyId && p.Status == "published")
                    .ConfigureAwait(false);

                if (!isPublished)
                {
                    // nếu không published => coi như không favorite
                    var ids0 = await GetFavoriteIdsInternalAsync(db, userId).ConfigureAwait(false);
                    return new FavoriteToggleResult
                    {
                        IsFavorite = false,
                        FavoriteIds = ids0,
                        FavoriteCount = ids0.Length
                    };
                }

                var isFavNow = await ToggleInternalAsync(db, userId, propertyId).ConfigureAwait(false);

                // summary (published)
                var ids = await GetFavoriteIdsInternalAsync(db, userId).ConfigureAwait(false);

                return new FavoriteToggleResult
                {
                    IsFavorite = isFavNow,
                    FavoriteIds = ids,
                    FavoriteCount = ids.Length
                };
            }
        }

        /// <summary>
        /// Toggle chỉ trả bool. Có dọn trùng và chỉ cho published.
        /// </summary>
        public async Task<bool> ToggleFavoriteAsync(int userId, int propertyId)
        {
            using (var db = new AppDbContext())
            {
                var isPublished = await db.Properties.AsNoTracking()
                    .AnyAsync(p => p.PropertyId == propertyId && p.Status == "published")
                    .ConfigureAwait(false);

                if (!isPublished) return false;

                return await ToggleInternalAsync(db, userId, propertyId).ConfigureAwait(false);
            }
        }

        // ==================== INTERNAL HELPERS ====================

        /// <summary>
        /// Toggle 1 property, đồng thời dọn trùng:
        /// - Nếu có nhiều dòng favorites trùng => chỉ giữ 1 dòng theo trạng thái mới, các dòng còn lại set Status=0
        /// - Update CreatedAt khi chuyển sang favorite (Status=1)
        /// </summary>
        private static async Task<bool> ToggleInternalAsync(AppDbContext db, int userId, int propertyId)
        {
            var list = await db.Favorites
                .Where(x => x.UserId == userId && x.PropertyId == propertyId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync()
                .ConfigureAwait(false);

            bool isFavorite;

            if (list == null || list.Count == 0)
            {
                var fav = new Favorite
                {
                    UserId = userId,
                    PropertyId = propertyId,
                    Status = 1,
                    CreatedAt = DateTime.Now
                };
                db.Favorites.Add(fav);
                isFavorite = true;
            }
            else
            {
                var main = list[0];

                // Toggle trạng thái trên dòng chính
                var newStatus = (short)(main.Status == 1 ? 0 : 1);
                main.Status = newStatus;
                if (newStatus == 1) main.CreatedAt = DateTime.Now;

                // ✅ Dọn các dòng trùng còn lại
                for (int i = 1; i < list.Count; i++)
                {
                    list[i].Status = 0;
                }

                isFavorite = (newStatus == 1);
            }

            await db.SaveChangesAsync().ConfigureAwait(false);
            return isFavorite;
        }

        private static async Task<int[]> GetFavoriteIdsInternalAsync(AppDbContext db, int userId)
        {
            var ids = await
                (from f in db.Favorites.AsNoTracking()
                 join p in db.Properties.AsNoTracking() on f.PropertyId equals p.PropertyId
                 where f.UserId == userId
                    && f.Status == 1
                    && p.Status == "published"
                 select f.PropertyId)
                .Distinct()
                .ToArrayAsync()
                .ConfigureAwait(false);

            return ids ?? new int[0];
        }
    }
}
