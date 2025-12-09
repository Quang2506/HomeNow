using System.ComponentModel.DataAnnotations;

namespace HomeNow.ViewModels
{
    public class LoginPhoneViewModel
    {
        [Required]
        [Display(Name = "Email / Số điện thoại")]
        public string LoginName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }
}
