using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using Web.Dto.Account;
using Web.Services.Abstractions;

public class UserPlugin
{
    private readonly IAccountService _accountService;
    private readonly ILogger<UserPlugin> _logger;

    public UserPlugin(IAccountService accountService, ILogger<UserPlugin> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [KernelFunction("create_user")]
    [Description("Create a new user in the system")]
    public async Task<string> CreateUserAsync(
        [Description("User full name")] string name,
        [Description("User email")] string email,
        [Description("User password")] string password)
    {
        _logger.LogWarning("TOOL CALLED: create_user name={Name} email={Email}", name, email);
        
        
        if (string.IsNullOrWhiteSpace(name))
            return "ERR: missing name";
        if (string.IsNullOrWhiteSpace(email))
            return "ERR: missing email";
        if (string.IsNullOrWhiteSpace(password))
            return "ERR: missing password";

        var dto = new RegisterInDto
        {
            Name = name,
            Email = email,
            Password = password,
            ConfirmPassword = password
        };

        var (response, status) = await _accountService.RegisterAsync(dto);

        _logger.LogWarning("TOOL RESULT: status={Status} message={Message}", status, response?.Message);

        if (status is >= 200 and < 300)
            return "OK: user created.";

        if (response?.Errors?.Any() == true)
            return "ERR: " + string.Join(" | ", response.Errors);

        return "ERR: " + (response?.Message ?? "unknown error");
    }
}