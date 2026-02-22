using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Dto.Account;
using Web.Services.Abstractions;

namespace Web.Controllers.v1;


/// <summary>
/// 
/// </summary>
public class AccountController(IAccountService accountService) : BaseController
{
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("Register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterInDto request, CancellationToken ct)
    {
        var(data, status) = await accountService.RegisterAsync(request, ct);
        return StatusCode(status, data);
    }
}
