using System.ComponentModel.DataAnnotations;

namespace OcpProduction.Web.ViewModels.Account;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "L'identifiant est obligatoire.")]
    [Display(Name = "Identifiant")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mot de passe")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Rester connecté")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
