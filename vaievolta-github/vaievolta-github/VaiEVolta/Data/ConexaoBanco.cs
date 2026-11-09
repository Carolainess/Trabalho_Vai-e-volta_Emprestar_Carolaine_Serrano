using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using VaiEVolta.Models;

namespace VaiEVolta.Data;

public static class ConexaoBanco
{
    // Lê a conexão de:
    //  1) DATABASE_URL (formato usado por Render, Railway, Heroku: postgres://usuario:senha@host:5432/banco)
    //  2) ConnectionStrings:Padrao (appsettings.json ou variável ConnectionStrings__Padrao no Docker)
    public static string Obter(IConfiguration config)
    {
        var url = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(url) && url.StartsWith("postgres"))
        {
            var uri = new Uri(url);
            var credenciais = uri.UserInfo.Split(':', 2);
            var porta = uri.Port > 0 ? uri.Port : 5432;
            return $"Host={uri.Host};Port={porta};Database={uri.AbsolutePath.TrimStart('/')};" +
                   $"Username={Uri.UnescapeDataString(credenciais[0])};Password={Uri.UnescapeDataString(credenciais.ElementAtOrDefault(1) ?? "")};" +
                   "SSL Mode=Prefer";
        }

        return config.GetConnectionString("Padrao")
               ?? throw new InvalidOperationException("Configure a conexão 'Padrao' ou a variável DATABASE_URL.");
    }

    // Aplica as migrations (cria/atualiza as tabelas) e coloca os dados fictícios.
    // Tenta algumas vezes porque, no Docker, o PostgreSQL pode demorar alguns segundos para subir.
    public static async Task PrepararAsync(WebApplication app)
    {
        var log = app.Logger;
        for (var tentativa = 1; tentativa <= 10; tentativa++)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
                SeedData.Inicializar(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>());
                log.LogInformation("Banco de dados pronto.");
                return;
            }
            catch (Exception ex) when (tentativa < 10)
            {
                log.LogWarning("Banco ainda não respondeu ({Tentativa}/10): {Erro}", tentativa, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
}

public class BancoHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;
    public BancoHealthCheck(AppDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default) =>
        await _db.Database.CanConnectAsync(ct)
            ? HealthCheckResult.Healthy("PostgreSQL conectado")
            : HealthCheckResult.Unhealthy("Sem conexão com o PostgreSQL");
}
