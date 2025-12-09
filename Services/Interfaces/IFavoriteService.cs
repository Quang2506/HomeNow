namespace HomeNow.Services.Interfaces
{
    public interface IFavoriteService
    {
        System.Collections.Generic.List<int> GetFavorites(int userId);
        void Toggle(int userId, int propertyId);
    }
}
