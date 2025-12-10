using System.Collections.Generic;
using System.Linq;
using Core.Models;
using Data;
using Services.Interfaces;

namespace Services.Implementations
{
    public class CityService : ICityService
    {
        public IList<City> GetActiveCities()
        {
            using (var db = new AppDbContext())
            {
                return db.Cities
                         .Where(c => c.IsActive)
                         .OrderBy(c => c.DisplayOrder)
                         .ToList();
            }
        }

        public City GetById(int id)
        {
            using (var db = new AppDbContext())
            {
                return db.Cities.FirstOrDefault(c => c.CityId == id);
            }
        }
    }
}
