using System.ComponentModel.DataAnnotations;

namespace Cinema.ViewModels
{
    public class ForgetPasswordVM
    {
        [Required(ErrorMessage = "Email or UserName is required")]
        public string UserNameOrEmail { get; set; } = string.Empty;
    }
}
