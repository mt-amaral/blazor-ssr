using Web.Dto;
using Web.Dto.Account;

namespace Web.Services.Abstractions;

public interface IAccountService
{

    Task<(Response<string?>, short)> DeleteAsync(long userId, CancellationToken ct);
    Task<(Response<CreateUserOutDto?>, short)> RegisterAsync(RegisterInDto request, CancellationToken ct);
    Task<(Response<LoginOutDto?>, short)> LoginAsync(LoginInDto request, CancellationToken ct);
    Task<(Response<UserOutDto?>, short)> GetUserByIdAsync(long id, CancellationToken ct);
    
    Task Logout();
    Task<(ResponsePage<UserOutDto?>, short)> ListUsersPaginatedAsync(UserFilterInDto filterInDto, CancellationToken ct);
}