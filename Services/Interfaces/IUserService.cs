using Core.Models;

namespace Services.Interfaces
{
    public interface IUserService
    {
        User GetOrCreateByPhone(string phoneNumber);
        User GetOrCreateByGoogle(string googleId, string email, string displayName);
        User GetById(int id);

        
        User Register(string email, string phoneNumber, string password);
        User ValidateLogin(string loginName, string password);
    }
}
