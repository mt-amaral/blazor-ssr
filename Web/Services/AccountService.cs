using Microsoft.AspNetCore.Identity;
using Web.Context;
using Web.Dto;
using Web.Dto.Account;
using Web.Entity.Identity;
using Web.Services.Abstractions;

namespace Web.Services;

public class AccountService(ApplicationDbContext context, UserManager<User> userManager) :  IAccountService
{
    
    public async Task<(Response<string?>, short)> RegisterAsync(RegisterInDto request)
    {
        try
        {
            var existing = await userManager.FindByEmailAsync(request.Email);
            if (existing != null)
                return (new Response<string?>(null, "Já existe um usuário registrado com esse email."), 400);
            
            var user = new User(userName: request.Email, email: request.Email);

            var statusCreate = await userManager.CreateAsync(user, request.Password);
            if (!statusCreate.Succeeded)
            {
                var errors = statusCreate.Errors.Select(e => e.Description).ToList();
                return (new Response<string?>($"Erro ao criar usuário {user.UserName}", errors), 400);
            }

            await context.SaveChangesAsync();
            return (new Response<string?>(null, $"Usuário {user.UserName} registrado com sucesso!"), 200);
        }
        catch
        {
            return (new Response<string?>(null, $"Erro na criação de usuário: {request.Email}"), 400);
        }
    }
}
