using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    [Table("users")] // ✅ lowercase đúng Postgres
    public class User
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("phone_number")]
        public string PhoneNumber { get; set; }

        [Column("google_id")]
        public string GoogleId { get; set; }

        [Column("display_name")]
        public string DisplayName { get; set; }

        [Column("status")]
        public string Status { get; set; } // pending/active/blocked...

        [Column("role")]
        public string Role { get; set; }   // user/admin...

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        [Column("password_hash")]
        public string PasswordHash { get; set; }

        // ===== Verify Email =====
        [Column("email_verified")]
        public bool EmailVerified { get; set; }

        [Column("email_verified_at")]
        public DateTime? EmailVerifiedAt { get; set; }

        // ===== OTP =====
        [Column("otp_code")]
        public string OtpCode { get; set; }

        [Column("otp_purpose")]
        public string OtpPurpose { get; set; } // VERIFY / RESET

        [Column("otp_sent_at")]
        public DateTime? OtpSentAt { get; set; }

        [Column("otp_expires_at")]
        public DateTime? OtpExpiresAt { get; set; }
    }
}
