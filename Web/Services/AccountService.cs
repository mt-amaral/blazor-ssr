using Microsoft.AspNetCore.Identity;
using Web.Context;
using Web.Dto;
using Web.Dto.Account;
using Web.Entity.Identity;
using Web.Services.Abstractions;

namespace Web.Services;

public class AccountService(ApplicationDbContext context, UserManager<User> userManager, SignInManager<User> signInMannger) :  IAccountService
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
    
    public async Task<(Response<LoginOutDto?>, short)> LoginAsync(LoginInDto request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return (new Response<LoginOutDto?>(null, "Credenciais inválidas"), 400);

        var result = await signInMannger.PasswordSignInAsync(
            user.UserName!, request.Password, request.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var dto = new LoginOutDto(
                Id: 0,
                FullName: "",  
                Email: user.Email ?? request.Email
            );

            return (new Response<LoginOutDto?>(dto, "OK"), 200);
        }

        if (result.RequiresTwoFactor)
        {
            var dto = new LoginOutDto(
                Id: 0,
                FullName: "",
                Email: user.Email ?? request.Email,
                RequiresTwoFactor: true
            );
            
            return (new Response<LoginOutDto?>(dto, "Requer 2FA"), 200);
        }

        if (result.IsLockedOut)
        {
            var dto = new LoginOutDto(
                Id: 0,
                FullName: "",
                Email: user.Email ?? request.Email,
                IsLockedOut: true
            );

            return (new Response<LoginOutDto?>(dto, "Conta bloqueada"), 200);
        }

        return (new Response<LoginOutDto?>(null, "Credenciais inválidas"), 400);
    }
    
    public Task Logout() => signInMannger.SignOutAsync();
    
}
