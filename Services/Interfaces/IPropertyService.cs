using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Models;
using Core.ViewModels;

namespace Services.Interfaces
{
    public interface IPropertyService
    {
        /// <summary>
        /// Tìm kiếm / lọc danh sách nhà.
        /// </summary>
        Task<List<PropertyListViewModel>> SearchAsync(
            string langCode,
            string listingType,
            int? cityId,
            string priceRange,
            string propertyType,
            string keyword);

        /// <summary>
        /// Lấy chi tiết 1 căn nhà (dùng cho VR / Detail).
        /// </summary>
        Task<Property> GetByIdAsync(int id);
    }
}
