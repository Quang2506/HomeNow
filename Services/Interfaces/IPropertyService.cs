using Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeNow.Services.Interfaces
{
    public interface IPropertyService
    {
        // ====== CÁC HÀM CŨ CỦA BẠN (NẾU CÓ) ĐỂ NGUYÊN Ở TRÊN ======
        // ví dụ:
        // Task<Property> GetByIdAsync(int id);

        // ====== HÀM MỚI DÙNG CHO MÀN DANH SÁCH NHÀ ======

        /// <summary>
        /// Lấy danh sách nhà (chưa map sang ViewModel), kèm theo bản dịch.
        /// </summary>
        Task<IList<Property>> GetAllWithTranslationsAsync();

        /// <summary>
        /// Lấy thông tin 1 nhà (cần cho màn VR).
        /// </summary>
        Task<Property> GetByIdAsync(int id);
    }
}
