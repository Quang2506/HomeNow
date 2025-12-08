using System;
using System.Threading.Tasks;
using Core.Models;
using Data;
using Services.Interfaces;

namespace Services.Implementations
{
    public class ViewingRequestService : IViewingRequestService
    {
        private readonly AppDbContext _db;

        public ViewingRequestService()
        {
            _db = new AppDbContext();
        }

        public async Task CreateAsync(ViewingRequest request)
        {
            request.Status = "new";
            request.CreatedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            _db.ViewingRequests.Add(request);
            await _db.SaveChangesAsync();
        }
    }
}
