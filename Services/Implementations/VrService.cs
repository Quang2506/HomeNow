using Core.Models;
using Data;
using HomeNow.Services.Interfaces;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace HomeNow.Services.Implementations
{
    public class VrService : IVrService
    {
        private readonly AppDbContext _db;

        public VrService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IList<VrScene>> GetScenesForPropertyAsync(int propertyId)
        {
            return await _db.VrScenes
                .AsNoTracking()
                .Where(s => s.PropertyId == propertyId)
                .Include(s => s.Translations)
                .Include(s => s.Hotspots.Select(h => h.Translations))
                .OrderByDescending(s => s.IsDefault)
                .ThenBy(s => s.Id)
                .ToListAsync();
        }
    }
}
