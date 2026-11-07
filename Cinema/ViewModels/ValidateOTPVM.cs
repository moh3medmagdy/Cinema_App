using System.ComponentModel.DataAnnotations;

namespace Cinema.ViewModels
{
    public class ValidateOTPVM
    {
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP is required")]
        public string OTP { get; set; } = string.Empty;
    }
}
