using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Models;   // PropertyListItemViewModel

namespace Services.Interfaces
{

    public class FavoriteSummaryResult
    {
        public int FavoriteCount { get; set; }
        public int[] FavoriteIds { get; set; }
    }

   
    public class FavoriteToggleResult : FavoriteSummaryResult
    {
        public bool IsFavorite { get; set; }
    }

    public interface IFavoriteService
    {
        Task<List<PropertyListItemViewModel>> GetFavoritesAsync(int userId, string langCode);

      
        Task<bool> ToggleFavoriteAsync(int userId, int propertyId);

       
        Task<int[]> GetFavoriteIdsAsync(int userId);

     
        Task<FavoriteToggleResult> ToggleFavoriteWithSummaryAsync(int userId, int propertyId, string langCode);
    }
}
