using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Core.Models;
using Data;
using Services.Interfaces;

namespace Services.Implementations
{
    public class UserService : IUserService
    {
       
        public User Register(string email, string phoneNumber, string password)
        {
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Email hoặc số điện thoại là bắt buộc.");

            using (var db = new AppDbContext())
            {
                // Kiểm tra trùng
                if (!string.IsNullOrEmpty(email) &&
                    db.Users.Any(u => u.Email == email))
                    throw new InvalidOperationException("Email đã được sử dụng.");

                if (!string.IsNullOrEmpty(phoneNumber) &&
                    db.Users.Any(u => u.PhoneNumber == phoneNumber))
                    throw new InvalidOperationException("Số điện thoại đã được sử dụng.");

                var displayName = GetDisplayName(email, phoneNumber);

                var user = new User
                {
                    Email = email,
                    PhoneNumber = phoneNumber,
                    DisplayName = displayName,
                    Status = "active",
                    Role = "user",
                    PasswordHash = HashPassword(password),
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                db.Users.Add(user);
                db.SaveChanges();
                return user;
            }
        }

      
        public User ValidateLogin(string loginName, string password)
        {
            if (string.IsNullOrWhiteSpace(loginName) || string.IsNullOrWhiteSpace(password))
                return null;

            using (var db = new AppDbContext())
            {
                var user = db.Users.FirstOrDefault(u =>
                    u.Email == loginName || u.PhoneNumber == loginName);

                if (user == null) return null;

                var hash = HashPassword(password);
                if (!string.Equals(hash, user.PasswordHash, StringComparison.Ordinal))
                    return null;

                user.LastLoginAt = DateTime.UtcNow;
                db.SaveChanges();

                return user;
            }
        }

       

        public User GetOrCreateByPhone(string phoneNumber)
        {
            using (var db = new AppDbContext())
            {
                var user = db.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
                if (user == null)
                {
                    user = new User
                    {
                        PhoneNumber = phoneNumber,
                        DisplayName = phoneNumber,
                        Status = "active",
                        Role = "user",
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };
                    db.Users.Add(user);
                }
                else
                {
                    if (string.IsNullOrEmpty(user.DisplayName))
                        user.DisplayName = user.PhoneNumber;

                    user.LastLoginAt = DateTime.UtcNow;
                }

                db.SaveChanges();
                return user;
            }
        }

        public User GetOrCreateByGoogle(string googleId, string email, string displayName)
        {
            using (var db = new AppDbContext())
            {
                var user = db.Users.FirstOrDefault(u => u.GoogleId == googleId);

                if (user == null)
                {
                    var finalDisplayName = !string.IsNullOrWhiteSpace(displayName)
                        ? displayName
                        : GetDisplayName(email, null) ?? googleId;

                    user = new User
                    {
                        GoogleId = googleId,
                        Email = email,
                        DisplayName = finalDisplayName,
                        Status = "active",
                        Role = "user",
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };

                    db.Users.Add(user);
                }
                else
                {
                    if (!string.IsNullOrEmpty(email))
                        user.Email = email;

                    if (string.IsNullOrEmpty(user.DisplayName))
                        user.DisplayName = GetDisplayName(user.Email, user.PhoneNumber) ?? user.DisplayName;

                    user.LastLoginAt = DateTime.UtcNow;
                }

                db.SaveChanges();
                return user;
            }
        }

        public User GetById(int id)
        {
            using (var db = new AppDbContext())
            {
                return db.Users.FirstOrDefault(u => u.Id == id);
            }
        }

  

        private string GetDisplayName(string email, string phone)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                var at = email.IndexOf("@", StringComparison.Ordinal);
                return at > 0 ? email.Substring(0, at) : email;
            }

            if (!string.IsNullOrWhiteSpace(phone))
                return phone;

            return null;
        }

        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = sha.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
