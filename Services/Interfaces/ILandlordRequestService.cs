using System.Threading.Tasks;
using Core.Models;

namespace Services.Interfaces
{
    public interface ILandlordRequestService
    {
        Task CreateAsync(LandlordRequest request);
    }
}
