using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Models;   // RẤT QUAN TRỌNG: dùng PropertyListItemViewModel bên Core.Models

namespace Services.Interfaces
{
    public interface IFavoriteService
    {
        Task<List<PropertyListItemViewModel>> GetFavoritesAsync(int userId, string langCode);

        Task<bool> ToggleFavoriteAsync(int userId, int propertyId);
    }
}
