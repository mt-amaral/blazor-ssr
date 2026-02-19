using Web.Entity.Identity;

namespace Web.Services.Abstractions;

public interface IUserLoggedService
{
    Task<User> GetUserLoggedAsync();
}

