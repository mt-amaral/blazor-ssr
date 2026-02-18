using Web.Dto;
using Web.Dto.Account;

namespace Web.Services.Abstractions;

public interface IAccountService
{

    Task<(Response<string?>, short)> DeleteAsync(long userId, CancellationToken ct);
    Task<(Response<string?>, short)> RegisterAsync(RegisterInDto request);
    Task<(Response<LoginOutDto?>, short)> LoginAsync(LoginInDto request);
    
    Task Logout();
    Task<(ResponsePage<UserOutDto?>, short)> ListUsersPaginatedAsync(UserFilterInDto filterInDto, CancellationToken ct);
}