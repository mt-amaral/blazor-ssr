using System.ComponentModel.DataAnnotations;

namespace Web.Dto.Account;

public sealed class RegisterInDto
{
    [Required(ErrorMessage = "Name is required")]
    [MinLength(6,  ErrorMessage = "Minimo 6 caracteres")]
    [MaxLength(50, ErrorMessage = "Maximo 50 caracteres")]
    public string Name { get; set; } = "";
    
    [Required(ErrorMessage = "Email é obrigatorio")] 
    [MaxLength(50, ErrorMessage = "Maximo 255 caracteres")]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Senha é obrigatorio")] 
    [MaxLength(50, ErrorMessage = "Maximo 255 caracteres")]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirmar senha é obrigatorio")] 
    [MaxLength(50, ErrorMessage = "Maximo 255 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = "";
}