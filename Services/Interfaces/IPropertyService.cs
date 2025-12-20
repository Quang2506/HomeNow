using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Models;
using Core.ViewModels;

namespace Services.Interfaces
{
    public interface IPropertyService
    {
        Task<List<PropertyListViewModel>> SearchAsync(
            string langCode,
            string listingType,
            int? cityId,
            string priceRange,
            string propertyType,
            string keyword);

        Task<PagedResult<PropertyListViewModel>> SearchPagedAsync(
            string langCode,
            string listingType,
            int? cityId,
            string priceRange,
            string propertyType,
            string keyword,
            int page,
            int pageSize);

        Task<Property> GetByIdAsync(int id);
    }
}
