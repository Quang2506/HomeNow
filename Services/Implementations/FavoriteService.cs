using Core.Models;
using Data;
using HomeNow.Services.Interfaces;
using Services.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace HomeNow.Services.Implementations
{
    public class FavoriteService : IFavoriteService
    {
        private readonly AppDbContext _db;

        // 👉 constructor NHẬN AppDbContext, đúng với cách bạn khởi tạo trong PropertyController
        public FavoriteService(AppDbContext db)
        {
            _db = db;
        }

        public List<int> GetFavorites(int userId)
        {
            return _db.UserFavoriteProperties
                      .Where(x => x.UserId == userId)
                      .Select(x => x.PropertyId)
                      .ToList();
        }

        public void Toggle(int userId, int propertyId)
        {
            var fav = _db.UserFavoriteProperties
                         .FirstOrDefault(x => x.UserId == userId && x.PropertyId == propertyId);

            if (fav == null)
            {
                _db.UserFavoriteProperties.Add(new UserFavoriteProperty
                {
                    UserId = userId,
                    PropertyId = propertyId
                });
            }
            else
            {
                _db.UserFavoriteProperties.Remove(fav);
            }

            _db.SaveChanges();
        }
    }
}
