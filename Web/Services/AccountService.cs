using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Context;
using Web.Dto;
using Web.Dto.Account;
using Web.Entity.Identity;
using Web.Services.Abstractions;

namespace Web.Services;

public class AccountService(ApplicationDbContext context, UserManager<User> userManager, SignInManager<User> signInMannger) :  IAccountService
{
    
    public async Task<(Response<string?>, short)> RegisterAsync(RegisterInDto request, CancellationToken ct)
    {
        try
        {
            var existing = await userManager.FindByEmailAsync(request.Email);
            if (existing != null)
                return (new Response<string?>(null, "Já existe um usuário registrado com esse email."), 400);
            
            var user = new User(userName: request.Name, email: request.Email);

            var statusCreate = await userManager.CreateAsync(user, request.Password);
            if (!statusCreate.Succeeded)
            {
                var errors = statusCreate.Errors.Select(e => e.Description).ToList();
                return (new Response<string?>($"Erro ao criar usuário {user.UserName}", errors), 400);
            }

            await context.SaveChangesAsync(ct);
            return (new Response<string?>(null, $"Usuário {user.UserName} registrado com sucesso!"), 200);
        }
        catch
        {
            return (new Response<string?>(null, $"Erro na criação de usuário: {request.Email}"), 400);
        }
    }
    
    public async Task<(Response<string?>, short)> DeleteAsync(long userId, CancellationToken ct)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return (new Response<string?>(null, "Usuário não encontrado."), 404);

            var result = await userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return (new Response<string?>($"Erro ao deletar usuário {user.UserName}", errors), 400);
            }

            await context.SaveChangesAsync(ct);

            return (new Response<string?>(null, $"Usuário {user.UserName} removido com sucesso!"), 200);
        }
        catch
        {
            return (new Response<string?>(null, $"Erro ao remover usuário: {userId}"), 400);
        }
    }

    
    public async Task<(Response<LoginOutDto?>, short)> LoginAsync(LoginInDto request, CancellationToken ct)
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
    
    
    
    public async Task<(ResponsePage<UserOutDto?>, short)> ListUsersPaginatedAsync(UserFilterInDto filterInDto, CancellationToken ct)
    {
        try
        {
            var query = context.Users.AsQueryable().AsNoTracking();
            
            if (!string.IsNullOrWhiteSpace(filterInDto.Name))
                query = query.Where(u => u.UserName.Contains(filterInDto.Name));
            if (!string.IsNullOrWhiteSpace(filterInDto.Email))
                query = query.Where(u => u.Email.Contains(filterInDto.Email));

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((filterInDto.Page - 1) * filterInDto.PageSize)
                .Take(filterInDto.PageSize)
                .ToListAsync(ct);

            var usersDto = items.Select(u => new UserOutDto(u.Id, u.UserName!, u.Email!)).ToList();

            var responsePage = new ResponsePage<UserOutDto?>(
                items: usersDto,
                totalCount: totalCount,
                currentPage: filterInDto.Page,
                pageSize: filterInDto.PageSize,
                message: null
            );

            return (responsePage, 200);
        }
        catch (Exception ex)
        {
            var errorResponse = new ResponsePage<UserOutDto?>(
                message: "Erro ao carregar usuários",
                errors: new List<string> { ex.Message }
            );
            return (errorResponse, 500);
        }
    }
    
}
