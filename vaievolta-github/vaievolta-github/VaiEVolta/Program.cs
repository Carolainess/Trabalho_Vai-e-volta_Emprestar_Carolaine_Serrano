using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaiEVolta.Data;
using VaiEVolta.Models;
using VaiEVolta.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- NUVEM: a porta e o banco vêm de variáveis de ambiente ----------
// Serviços como Render e Railway informam a porta em PORT
var porta = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(porta))
    builder.WebHost.UseUrls($"http://0.0.0.0:{porta}");

var conexao = ConexaoBanco.Obter(builder.Configuration);

// ---------- Serviços ----------
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(conexao));

builder.Services.AddScoped<EmprestimoService>();
builder.Services.AddSingleton<FotoService>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services.AddControllersWithViews(o =>
        o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute())) // proteção contra CSRF nos formulários
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// Login com cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Conta/Entrar";
        o.LogoutPath = "/Conta/Sair";
        o.AccessDeniedPath = "/Conta/AcessoNegado";
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = true;
        // Na API, em vez de redirecionar para a tela de login, responde 401/403
        o.Events.OnRedirectToLogin = ctx => RespostaApi(ctx, StatusCodes.Status401Unauthorized);
        o.Events.OnRedirectToAccessDenied = ctx => RespostaApi(ctx, StatusCodes.Status403Forbidden);
    });
builder.Services.AddAuthorization();

// Guarda as chaves de criptografia do cookie numa pasta (o login não cai quando o contêiner reinicia)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "chaves")))
    .SetApplicationName("VaiEVolta");

// Na nuvem a aplicação fica atrás de um proxy HTTPS
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddHealthChecks().AddCheck<BancoHealthCheck>("banco");

var app = builder.Build();

// ---------- Banco: aplica as migrations e insere os dados de demonstração ----------
await ConexaoBanco.PrepararAsync(app);

// ---------- Pipeline ----------
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Erro");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Home/Erro/{0}");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health"); // usado pela nuvem para saber se o site está no ar
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static Task RespostaApi(Microsoft.AspNetCore.Authentication.RedirectContext<CookieAuthenticationOptions> ctx, int status)
{
    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new
        {
            erro = status == 401 ? "Faça login para usar este endpoint." : "Você não tem permissão para isso."
        });
    }
    ctx.Response.Redirect(ctx.RedirectUri);
    return Task.CompletedTask;
}
