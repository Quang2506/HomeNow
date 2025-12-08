using Core.Models;
using Data;
using HomeNow.Services.Interfaces;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace HomeNow.Services.Implementations
{
    public class PropertyService : IPropertyService
    {
        private readonly AppDbContext _db;

        public PropertyService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Lấy tất cả nhà kèm theo bản dịch.
        /// </summary>
        public async Task<IList<Property>> GetAllWithTranslationsAsync()
        {
            return await _db.Properties
                .Include(p => p.Translations)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy 1 nhà theo Id.
        /// </summary>
        public async Task<Property> GetByIdAsync(int id)
        {
            return await _db.Properties
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // ====== CÁC HÀM CŨ CỦA BẠN (NẾU CÓ) VIẾT THÊM Ở DƯỚI NÀY ======
    }
}
