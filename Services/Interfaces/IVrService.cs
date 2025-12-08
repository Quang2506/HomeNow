using Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeNow.Services.Interfaces
{
    public interface IVrService
    {
        /// <summary>
        /// Lấy toàn bộ scene + hotspot + bản dịch cho 1 căn nhà.
        /// </summary>
        Task<IList<VrScene>> GetScenesForPropertyAsync(int propertyId);
    }
}
