using System.ComponentModel.DataAnnotations;

namespace Template.Web.Features.Login
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Inserisci l'email")]
        [EmailAddress(ErrorMessage = "Inserisci un indirizzo email valido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Inserisci la password")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }
}