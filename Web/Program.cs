using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using Newtonsoft.Json;
using Web;
using Web.Components;
using Web.Context;
using Web.Entity.Identity;
using Web.Middleware;
using Web.Services;
using Web.Services.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// MudBlazor + Razor Components
builder.Services.AddMudServices();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

builder.Services.AddCascadingAuthenticationState();

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsDev", policy => policy
        .WithOrigins(Configuration.LocalhostCors)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());

    options.AddPolicy("CorsProd", policy => policy
        .WithOrigins(Configuration.ProdHostCors)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };
});



// services
builder.Services.AddScoped<IAccountService, AccountService>();

// Controllers + auth global
builder.Services.AddControllers(options =>
{
    var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
})
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    c.SwaggerDoc("v1", new()
    {
        Title = "SNCore Documentação Api",
        Description = ""
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<User>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseCors("CorsDev");
    app.UseMigrationsEndPoint();
    app.UseStaticFiles();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin API V1");
        c.InjectStylesheet("/swagger-ui/SwaggerDark.css");
    });
}
else if (app.Environment.IsStaging())
{
    app.UseCors("CorsProd");

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// >>> FALTAVA NO SEU PIPELINE <<<
app.UseAuthentication();
app.UseAuthorization();

// Se você quiser manter seu /404 de UI, pode deixar ligado
app.UseStatusCodePagesWithRedirects("/404");

// Seu middleware de exceção (ok ficar aqui também)
app.UseMiddleware<ExceptionMiddleware>();

app.UseAntiforgery();

// Endpoints
app.MapControllers();

// >>> ISSO AQUI RESOLVE O 200+HTML NO /api QUANDO NÃO EXISTE ENDPOINT <<<
app.MapFallback("/api/{*path}", () => Results.NotFound());

// Static + Blazor por último (fallback final)
app.MapStaticAssets();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
