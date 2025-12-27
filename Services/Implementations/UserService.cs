using Core.Models;
using Core.Resources;
using Data;
using Services.Interfaces;
using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _db;

        private const int OTP_LENGTH = 6;
        private static readonly TimeSpan OTP_EXPIRE = TimeSpan.FromMinutes(5);

        public UserService() : this(new AppDbContext()) { }

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        public User GetById(int id)
        {
            return _db.Users.FirstOrDefault(x => x.Id == id);
        }

        public bool IsEmailExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var e = email.Trim().ToLowerInvariant();
            return _db.Users.Any(x => x.Email != null && x.Email.ToLower() == e);
        }

        public bool IsPhoneExists(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            var p = phone.Trim();
            return _db.Users.Any(x => x.PhoneNumber != null && x.PhoneNumber == p);
        }

        public User CreateUser(string email, string phone, string displayName, string password)
        {
            var now = DateTime.Now;

            var user = new User
            {
                Email = email != null ? email.Trim() : null,
                PhoneNumber = phone != null ? phone.Trim() : null,
                DisplayName = displayName != null ? displayName.Trim() : null,
                PasswordHash = HashPassword(password),

                Role = "user",
                Status = "pending",
                CreatedAt = now,

                EmailVerified = false
            };

            _db.Users.Add(user);
            _db.SaveChanges();
            return user;
        }

        public User ValidateLogin(string loginName, string password)
        {
            if (string.IsNullOrWhiteSpace(loginName) || string.IsNullOrWhiteSpace(password))
                return null;

            var key = loginName.Trim();
            var passHash = HashPassword(password);

            var user = _db.Users.FirstOrDefault(x =>
                (x.Email != null && x.Email == key) ||
                (x.PhoneNumber != null && x.PhoneNumber == key)
            );

            if (user == null) return null;
            if (!string.Equals(user.PasswordHash, passHash, StringComparison.Ordinal)) return null;

            return user;
        }

        // OTP - VERIFY EMAIL
        public bool SendVerifyEmailOtp(string email, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(email))
            {
                error = AuthTexts.Msg_OtpInvalid;
                return false;
            }

            var e = email.Trim();
            var user = _db.Users.FirstOrDefault(x => x.Email == e);
            if (user == null)
            {
                error = AuthTexts.Msg_OtpInvalid;
                return false;
            }

            var otp = GenerateOtp();
            user.OtpCode = otp;
            user.OtpPurpose = "VERIFY";
            user.OtpSentAt = DateTime.Now;
            user.OtpExpiresAt = DateTime.Now.Add(OTP_EXPIRE);

            _db.SaveChanges();

            return SendEmailOtp(e, otp, "VERIFY", out error);
        }

        public bool ResendVerifyEmailOtp(string email, out string error)
        {
            return SendVerifyEmailOtp(email, out error);
        }

        public bool VerifyEmailOtp(string email, string code, out User user, out string error)
        {
            user = null;
            error = null;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                error = AuthTexts.Msg_OtpInvalid;
                return false;
            }

            var e = email.Trim();
            var c = code.Trim();

            user = _db.Users.FirstOrDefault(x => x.Email == e);
            if (user == null)
            {
                error = AuthTexts.Msg_OtpInvalid;
                return false;
            }

            if (!string.Equals(user.OtpPurpose, "VERIFY", StringComparison.OrdinalIgnoreCase))
            {
                error = AuthTexts.Msg_OtpInvalid;
                return false;
            }

            if (user.OtpExpiresAt == null || user.OtpExpiresAt.Value < DateTime.Now)
            {
                error = AuthTexts.Msg_OtpExpired;
                return false;
            }

            if (!string.Equals(user.OtpCode, c, StringComparison.Ordinal))
            {
                error = AuthTexts.Msg_OtpInvalid;
                return false;
            }

            user.EmailVerified = true;
            user.EmailVerifiedAt = DateTime.Now;
            user.Status = "active";

            user.OtpCode = null;
            user.OtpPurpose = null;
            user.OtpExpiresAt = null;

            _db.SaveChanges();
            return true;
        }

        // OTP - RESET PASSWORD
        public bool SendResetPasswordOtp(string email, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(email))
            {
                error = AuthTexts.Msg_OtpInvalid;
                return false;
            }

            var e = email.Trim();
            var user = _db.Users.FirstOrDefault(x => x.Email == e);
            if (user == null)
            {
                error = AuthTexts.Msg_OtpInvalid;
                return false;
            }

            var otp = GenerateOtp();
            user.OtpCode = otp;
            user.OtpPurpose = "RESET";
            user.OtpSentAt = DateTime.Now;
            user.OtpExpiresAt = DateTime.Now.Add(OTP_EXPIRE);

            _db.SaveChanges();

            return SendEmailOtp(e, otp, "RESET", out error);
        }

        public bool ResetPasswordByOtp(string email, string code, string newPassword, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                error = AuthTexts.Msg_OtpInvalid;
                return false;
            }

            var e = email.Trim();
            var c = code.Trim();

            var user = _db.Users.FirstOrDefault(x => x.Email == e);
            if (user == null)
            {
                error = AuthTexts.Msg_OtpInvalid;
                return false;
            }

            if (!string.Equals(user.OtpPurpose, "RESET", StringComparison.OrdinalIgnoreCase))
            {
                error = AuthTexts.Msg_OtpInvalid;
                return false;
            }

            if (user.OtpExpiresAt == null || user.OtpExpiresAt.Value < DateTime.Now)
            {
                error = AuthTexts.Msg_OtpExpired;
                return false;
            }

            if (!string.Equals(user.OtpCode, c, StringComparison.Ordinal))
            {
                error = AuthTexts.Msg_OtpInvalid;
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);

            user.OtpCode = null;
            user.OtpPurpose = null;
            user.OtpExpiresAt = null;

            _db.SaveChanges();
            return true;
        }

        // SMTP send
        public bool SendEmailOtp(string toEmail, string code, string purpose, out string error)
        {
            error = null;

            try
            {
                var host = ConfigurationManager.AppSettings["SmtpHost"];
                var port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                var user = ConfigurationManager.AppSettings["SmtpUser"];
                var pass = ConfigurationManager.AppSettings["SmtpPass"];
                var fromName = ConfigurationManager.AppSettings["SmtpFrom"] ?? "NhaNow";

                var subject = string.Equals(purpose, "RESET", StringComparison.OrdinalIgnoreCase)
                    ? AuthTexts.Email_Subject_Reset
                    : AuthTexts.Email_Subject_Verify;

                var bodyHtml = BuildOtpHtml(code, purpose);

                using (var msg = new MailMessage())
                {
                    msg.From = new MailAddress(user, fromName, Encoding.UTF8);
                    msg.To.Add(new MailAddress(toEmail));
                    msg.Subject = subject;
                    msg.SubjectEncoding = Encoding.UTF8;
                    msg.Body = bodyHtml;
                    msg.IsBodyHtml = true;
                    msg.BodyEncoding = Encoding.UTF8;

                    using (var client = new SmtpClient(host, port))
                    {
                        client.EnableSsl = true;
                        client.Credentials = new NetworkCredential(user, pass);
                        client.Send(msg);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string BuildOtpHtml(string code, string purpose)
        {
            var title = string.Equals(purpose, "RESET", StringComparison.OrdinalIgnoreCase)
                ? AuthTexts.Forgot_Title
                : AuthTexts.Otp_Title;

            return @"
            <div style='font-family:Arial,sans-serif;max-width:520px;margin:0 auto'>
              <h2 style='margin:0 0 12px 0;color:#0f172a'>" + WebUtility.HtmlEncode(title) + @"</h2>
              <p style='margin:0 0 12px 0;color:#334155'>OTP:</p>
              <div style='font-size:28px;font-weight:800;letter-spacing:4px;padding:12px 16px;background:#f1f5f9;border-radius:12px;display:inline-block;color:#0f172a'>" + WebUtility.HtmlEncode(code) + @"</div>
              <p style='margin:12px 0 0 0;color:#64748b'>" + WebUtility.HtmlEncode(AuthTexts.Msg_OtpExpiresIn5Min) + @"</p>
            </div>";
        }

        private static string GenerateOtp()
        {
            var bytes = new byte[OTP_LENGTH];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);

            var sb = new StringBuilder();
            for (int i = 0; i < OTP_LENGTH; i++)
                sb.Append((bytes[i] % 10).ToString());

            return sb.ToString();
        }

        private static string HashPassword(string password)
        {
            if (password == null) password = "";
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

     
        //  Change password by old password (không ảnh hưởng OTP)
      
        public bool ChangePassword(int userId, string oldPassword, string newPassword, out string error)
        {
            error = null;

            try
            {
                if (userId <= 0)
                {
                    error = "Invalid user.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
                {
                    error = "Vui lòng nhập đầy đủ thông tin.";
                    return false;
                }

                var user = _db.Users.FirstOrDefault(x => x.Id == userId);
                if (user == null)
                {
                    error = "User not found.";
                    return false;
                }

                var oldHash = HashPassword(oldPassword);
                if (!string.Equals(user.PasswordHash, oldHash, StringComparison.Ordinal))
                {
                    error = "Mật khẩu cũ không đúng.";
                    return false;
                }

                user.PasswordHash = HashPassword(newPassword);
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
        public bool UpdateProfile(int userId, string displayName, string phoneNumber, out string error)
        {
            error = null;

            try
            {
                if (userId <= 0)
                {
                    error = "Invalid user.";
                    return false;
                }

                var user = _db.Users.FirstOrDefault(x => x.Id == userId);
                if (user == null)
                {
                    error = "User not found.";
                    return false;
                }

                var name = (displayName ?? "").Trim();
                var phone = (phoneNumber ?? "").Trim();

                // Optional: bắt buộc nhập tên
                if (string.IsNullOrWhiteSpace(name))
                {
                    error = "Vui lòng nhập họ và tên.";
                    return false;
                }

                //  nếu có phone thì check trùng
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    var phoneExists = _db.Users.Any(x => x.Id != userId && x.PhoneNumber != null && x.PhoneNumber == phone);
                    if (phoneExists)
                    {
                        error = AuthTexts.Msg_PhoneExists; 
                        return false;
                    }
                }

                user.DisplayName = name;
                user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone;

                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

    }
}
