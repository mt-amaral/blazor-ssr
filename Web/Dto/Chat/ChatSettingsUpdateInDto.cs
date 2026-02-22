using System.ComponentModel.DataAnnotations;

namespace Web.Dto.Chat;

public record ChatSettingsUpdateInDto(
    [Required]
    string Content
    );