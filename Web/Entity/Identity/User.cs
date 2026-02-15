using Microsoft.AspNetCore.Identity;

namespace Web.Entity.Identity;

public sealed class User : IdentityUser<long>
{

    public User()
    {

    }
    public User(string userName, string email)
    {
        Email = email;
        UserName = userName ?? throw new ArgumentNullException(nameof(userName));
    }
    
}

