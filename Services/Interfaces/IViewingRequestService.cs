using System.Threading.Tasks;
using Core.Models;

namespace Services.Interfaces
{
    public interface IViewingRequestService
    {
        Task CreateAsync(ViewingRequest request);
    }
}
