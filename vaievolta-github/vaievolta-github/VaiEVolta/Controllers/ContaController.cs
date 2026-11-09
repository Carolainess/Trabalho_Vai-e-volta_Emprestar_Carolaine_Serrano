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

namespace VaiEVolta.Controllers;

public class ContaController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Usuario> _hasher;

    public ContaController(AppDbContext db, IPasswordHasher<Usuario> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // ---------- Cadastro ----------
    [HttpGet]
    public IActionResult Cadastro() =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Objetos") : View(new CadastroViewModel());

    [HttpPost]
    public async Task<IActionResult> Cadastro(CadastroViewModel vm)
    {
        var email = vm.Email.Trim().ToLowerInvariant();
        if (await _db.Usuarios.AnyAsync(u => u.Email == email))
            ModelState.AddModelError(nameof(vm.Email), "Já existe uma conta com este e-mail.");

        if (!ModelState.IsValid) return View(vm);

        var usuario = new Usuario
        {
            Nome = vm.Nome.Trim(),
            Email = email,
            Curso = vm.Curso?.Trim(),
            Telefone = vm.Telefone?.Trim()
        };
        usuario.SenhaHash = _hasher.HashPassword(usuario, vm.Senha);
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        await EntrarComoAsync(usuario);
        TempData["Sucesso"] = $"Bem-vinda(o), {usuario.Nome.Split(' ')[0]}! Sua conta foi criada.";
        return RedirectToAction("Index", "Objetos");
    }

    // ---------- Login ----------
    [HttpGet]
    public IActionResult Entrar(string? returnUrl) =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Objetos") : View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Entrar(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        if (usuario is null ||
            _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, vm.Senha) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "E-mail ou senha incorretos.");
            return View(vm);
        }

        await EntrarComoAsync(usuario);
        TempData["Sucesso"] = $"Olá de novo, {usuario.Nome.Split(' ')[0]}!";

        // Só redireciona para endereços do próprio site (evita redirecionamento malicioso)
        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)) return Redirect(vm.ReturnUrl);
        return RedirectToAction("Index", "Objetos");
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Sucesso"] = "Você saiu da sua conta. Até logo!";
        return RedirectToAction("Index", "Home");
    }

    // ---------- Perfil ----------
    [HttpGet, Authorize]
    public async Task<IActionResult> Perfil()
    {
        var usuario = await _db.Usuarios.FindAsync(User.Id());
        if (usuario is null) return await SairForcadoAsync();
        return View(await MontarPerfilAsync(usuario));
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Perfil(PerfilViewModel vm)
    {
        var usuario = await _db.Usuarios.FindAsync(User.Id());
        if (usuario is null) return await SairForcadoAsync();

        if (!ModelState.IsValid)
        {
            var completo = await MontarPerfilAsync(usuario);
            completo.Nome = vm.Nome; completo.Curso = vm.Curso; completo.Telefone = vm.Telefone;
            return View(completo);
        }

        usuario.Nome = vm.Nome.Trim();
        usuario.Curso = vm.Curso?.Trim();
        usuario.Telefone = vm.Telefone?.Trim();
        await _db.SaveChangesAsync();

        await EntrarComoAsync(usuario); // atualiza o nome que aparece no menu
        TempData["Sucesso"] = "Perfil atualizado!";
        return RedirectToAction(nameof(Perfil));
    }

    public IActionResult AcessoNegado() => View();

    // ---------- Auxiliares ----------
    private async Task<PerfilViewModel> MontarPerfilAsync(Usuario u) => new()
    {
        Nome = u.Nome,
        Email = u.Email,
        Curso = u.Curso,
        Telefone = u.Telefone,
        MembroDesde = u.CriadoEm,
        Admin = u.Perfil == Models.Perfil.Administrador,
        TotalObjetos = await _db.Objetos.CountAsync(o => o.DonoId == u.Id),
        TotalEmprestados = await _db.Emprestimos.CountAsync(e => e.Objeto!.DonoId == u.Id &&
            (e.Status == StatusEmprestimo.Emprestado || e.Status == StatusEmprestimo.Devolvido)),
        TotalPedidos = await _db.Emprestimos.CountAsync(e => e.SolicitanteId == u.Id)
    };

    // Guarda no cookie quem é a pessoa e qual o perfil dela (usuário ou administrador)
    private async Task EntrarComoAsync(Usuario u)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.Nome),
            new(ClaimTypes.Email, u.Email),
            new(ClaimTypes.Role, u.Perfil.ToString()),
            new("Iniciais", u.Iniciais)
        };
        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidade), new AuthenticationProperties { IsPersistent = true });
    }

    private async Task<IActionResult> SairForcadoAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Entrar));
    }
}
