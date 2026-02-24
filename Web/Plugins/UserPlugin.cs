using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Web.Context;
using Web.Dto;
using Web.Dto.Account;
using Web.Entity.Identity;
using Web.Services.Abstractions;

namespace Web.Plugins;

public class UserPlugin : IPlugin
{
    private readonly IAccountService _accountService;
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserPlugin> _logger;

    public string PluginName => "UserTools";
    public string Description => "Ferramentas para gerenciamento de usuários do sistema (criar, listar, deletar usuarios).";

    public UserPlugin(
        IAccountService accountService,
        ILogger<UserPlugin> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [KernelFunction("create_user")]
    [Description("Creates a new user account in the system.Returns user data and message ")]
    public async Task<(CreateUserOutDto?, string?)> CreateUserAsync(
        [Description("Full name")] string name,
        [Description("Valid email address")] string email,
        [Description("Strong password")] string password,
        CancellationToken ct = default)
    {
        _logger.LogInformation($" IA -> Criação de usuário: {email}, {name} ");
        var dto = new RegisterInDto { Name = name, Email = email, Password = password, ConfirmPassword = password };
        var (response, status) = await _accountService.RegisterAsync(dto, ct);
        return (response.Data, response.Message);
    }

    [KernelFunction("delete_user")]
    [Description("Remove um usuário do sistema permanentemente através do ID.")]
    public async Task<string?> DeleteUserAsync(
        [Description("ID numérico do usuário")] long userId, 
        CancellationToken ct = default)
    {
        _logger.LogInformation($"IA -> Deletando UsuarioId {userId}");
        var (response, status) = await _accountService.DeleteAsync(userId, ct);
        return response.Message;
    }

    [KernelFunction("list_users")]
    [Description("Lista e filtra usuários cadastrados com paginação.")]
    public async Task<ResponsePage<UserOutDto?>> ListUsersAsync(
        [Description("Filtro por nome (opcional)")] string? name = null,
        [Description("Filtro por e-mail (opcional)")] string? email = null,
        [Description("Número da página")] int page = 1,
        [Description("Quantidade por página")] int pageSize = 10,
        CancellationToken ct = default)
    {
        _logger.LogInformation($"IA -> Listando Usuario");
        var request = new UserFilterInDto() { Email = email, Name = name, PageSize = pageSize, Page = page };
        var (response, status) = await _accountService.ListUsersPaginatedAsync(request, ct);
        return response;
    }
    
    [KernelFunction("get_user")]
    [Description("Consulta dados de um  usuário do sistema através do ID.")]
    public async Task<Response<UserOutDto?>> GetUserAsync(
        [Description("id do usuario")] long userId,
        CancellationToken ct = default)
    {
        _logger.LogInformation($"IA -> Listando Usuario");
        var (response, status) = await _accountService.GetUserByIdAsync(userId, ct);
        return response;
    }
}