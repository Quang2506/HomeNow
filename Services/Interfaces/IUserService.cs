using Core.Models;

namespace Services.Interfaces
{
    public interface IUserService
    {
        User GetById(int id);

        bool IsEmailExists(string email);
        bool IsPhoneExists(string phone);

        // Register & Login
        User CreateUser(string email, string phone, string displayName, string password);
        User ValidateLogin(string loginName, string password);

        // OTP Verify Email
        bool SendVerifyEmailOtp(string email, out string error);
        bool ResendVerifyEmailOtp(string email, out string error);
        bool VerifyEmailOtp(string email, string code, out User user, out string error);

        // Forgot password
        bool SendResetPasswordOtp(string email, out string error);
        bool ResetPasswordByOtp(string email, string code, string newPassword, out string error);
        bool ChangePassword(int userId, string oldPassword, string newPassword, out string error);
        bool UpdateProfile(int userId, string displayName, string phoneNumber, out string error);
    }
}
