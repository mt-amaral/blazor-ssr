using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers.v1;


/// <summary>
/// 
/// </summary>
public class AccountController : BaseController
{
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("teste")]
    public Task<IActionResult> Teste()
    {
        return Task.FromResult<IActionResult>(Ok("teste"));
    }
}