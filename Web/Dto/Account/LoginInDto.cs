using System.ComponentModel.DataAnnotations;

namespace Web.Dto.Account;

public class LoginInDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }
}