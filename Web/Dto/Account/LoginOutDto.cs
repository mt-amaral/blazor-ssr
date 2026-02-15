namespace Web.Dto.Account;

public record LoginOutDto(
    long Id,
    string FullName,
    string Email,
    bool RequiresTwoFactor = false,
    bool IsLockedOut = false
);
