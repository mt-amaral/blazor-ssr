using Microsoft.AspNetCore.Identity;
using Web.Entity.Identity;
using Web.Services.Abstractions;

namespace Web.Services;

public class UserLoggedService: IUserLoggedService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<User> _userManager;

    public UserLoggedService(IHttpContextAccessor httpContextAccessor, UserManager<User> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public async Task<User> GetUserLoggedAsync() =>
    await _userManager.GetUserAsync(_httpContextAccessor.HttpContext?.User!)
    ?? throw new Exception("Erro ao obter usuario logado!");
}
