using System.Collections.Generic;
using Core.Models;

namespace Services.Interfaces
{
    public interface ICityService
    {
        IList<City> GetActiveCities();
        City GetById(int id);
    }
}
