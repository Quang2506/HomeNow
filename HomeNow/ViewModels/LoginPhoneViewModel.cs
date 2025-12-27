using Core.Resources;
using System.ComponentModel.DataAnnotations;

namespace HomeNow.ViewModels
{
    public class LoginPhoneViewModel
    {
        [Required(ErrorMessageResourceType = typeof(AuthTexts), ErrorMessageResourceName = "Msg_Required")]
        [Display(ResourceType = typeof(AuthTexts), Name = "Login_EmailOrPhone")]
        public string LoginName { get; set; }

        [Required(ErrorMessageResourceType = typeof(AuthTexts), ErrorMessageResourceName = "Msg_Required")]
        [Display(ResourceType = typeof(AuthTexts), Name = "Login_Password")]
        public string Password { get; set; }
    }
}
