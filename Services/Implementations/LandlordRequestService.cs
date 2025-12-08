using System;
using System.Threading.Tasks;
using Core.Models;
using Data;
using Services.Interfaces;

namespace Services.Implementations
{
    public class LandlordRequestService : ILandlordRequestService
    {
        private readonly AppDbContext _db;

        public LandlordRequestService()
        {
            _db = new AppDbContext();
        }

        public async Task CreateAsync(LandlordRequest request)
        {
            request.Status = "new";
            request.CreatedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            _db.LandlordRequests.Add(request);
            await _db.SaveChangesAsync();
        }
    }
}
