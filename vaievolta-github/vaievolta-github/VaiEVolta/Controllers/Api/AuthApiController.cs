using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaiEVolta.Data;
using VaiEVolta.Helpers;
using VaiEVolta.Models;
using VaiEVolta.Models.ViewModels;

namespace VaiEVolta.Controllers.Api;

/// <summary>
/// Login pela API (para testar no Postman/Insomnia): devolve o cookie de sessão.
/// </summary>
[ApiController]
[Route("api/auth")]
[IgnoreAntiforgeryToken]
public class AuthApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Usuario> _hasher;

    public AuthApiController(AppDbContext db, IPasswordHasher<Usuario> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // POST /api/auth/login  { "email": "carol@vaievolta.com", "senha": "Senha@123" }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginApiDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.Email == email);
        if (u is null || _hasher.VerifyHashedPassword(u, u.SenhaHash, dto.Senha) == PasswordVerificationResult.Failed)
            return Unauthorized(new { erro = "E-mail ou senha incorretos." });

        var identidade = new ClaimsIdentity(new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.Nome),
            new(ClaimTypes.Email, u.Email),
            new(ClaimTypes.Role, u.Perfil.ToString()),
            new("Iniciais", u.Iniciais)
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidade));

        return Ok(new { mensagem = "Login feito.", usuario = new { u.Id, u.Nome, u.Email, Perfil = u.Perfil.ToString() } });
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    // GET /api/auth/eu  -> quem está logado
    [HttpGet("eu"), Authorize]
    public IActionResult Eu() => Ok(new
    {
        id = User.Id(),
        nome = User.Identity!.Name,
        email = User.FindFirstValue(ClaimTypes.Email),
        perfil = User.EhAdmin() ? "Administrador" : "Usuario"
    });
}
