using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Web.Context;
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
        UserManager<User> userManager, 
        ApplicationDbContext context,
        ILogger<UserPlugin> logger)
    {
        _accountService = accountService;
        _userManager = userManager;
        _context = context;
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
        _logger.LogInformation($"(IA)..Criação de usuário: {email}, {name} ");
        var dto = new RegisterInDto { Name = name, Email = email, Password = password, ConfirmPassword = password };
        var (response, status) = await _accountService.RegisterAsync(dto, ct);
        return (response.Data, response.Message);
    }

    [KernelFunction("delete_user")]
    [Description("Remove um usuário do sistema permanentemente através do ID.")]
    public async Task<string> DeleteUserAsync(
        [Description("ID numérico do usuário")] long userId, 
        CancellationToken ct = default)
    {
        _logger.LogWarning("Solicitação de exclusão para ID: {UserId}", userId);
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return "ERR: Usuário não encontrado.";

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return $"ERR: Erro ao deletar {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}";
            }

            await _context.SaveChangesAsync(ct);
            return $"OK: Usuário {user.UserName} removido com sucesso!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover ID {UserId}", userId);
            return $"ERR: Erro interno ao remover usuário.";
        }
    }

    [KernelFunction("list_users")]
    [Description("Lista e filtra usuários cadastrados com paginação.")]
    public async Task<string> ListUsersAsync(
        [Description("Filtro por nome (opcional)")] string? name = null,
        [Description("Filtro por e-mail (opcional)")] string? email = null,
        [Description("Número da página")] int page = 1,
        [Description("Quantidade por página")] int pageSize = 10,
        CancellationToken ct = default)
    {
        try
        {
            var query = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(u => u.UserName!.Contains(name));
            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(u => u.Email!.Contains(email));

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new { u.Id, u.UserName, u.Email })
                .ToListAsync(ct);

            // Retornamos um resumo em string para a IA conseguir ler
            if (!items.Any()) return "Informação: Nenhum usuário encontrado para os filtros aplicados.";

            var result = new
            {
                Total = totalCount,
                PaginaAtual = page,
                Usuarios = items
            };

            // Retornar JSON serializado para que a IA possa interpretar os dados e responder ao usuário
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar usuários");
            return "ERR: Não foi possível carregar a lista de usuários.";
        }
    }
}