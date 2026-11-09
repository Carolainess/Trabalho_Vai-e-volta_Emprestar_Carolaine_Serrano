using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaiEVolta.Data;
using VaiEVolta.Models;
using VaiEVolta.Services;

namespace VaiEVolta.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly EmprestimoService _emprestimos;

    public HomeController(AppDbContext db, EmprestimoService emprestimos)
    {
        _db = db;
        _emprestimos = emprestimos;
    }

    public async Task<IActionResult> Index()
    {
        // Destaques: os objetos disponíveis mais recentes
        ViewBag.Destaques = await _db.Objetos
            .Include(o => o.Dono)
            .Where(o => o.Disponivel)
            .OrderByDescending(o => o.CriadoEm)
            .Take(6)
            .ToListAsync();
        ViewBag.Situacoes = await _emprestimos.SituacoesAsync();
        ViewBag.TotalObjetos = await _db.Objetos.CountAsync();
        ViewBag.TotalUsuarios = await _db.Usuarios.CountAsync(u => u.Perfil == Perfil.Usuario);
        ViewBag.TotalEmprestimos = await _db.Emprestimos.CountAsync(e =>
            e.Status == StatusEmprestimo.Emprestado || e.Status == StatusEmprestimo.Devolvido);
        return View();
    }

    // Página que documenta e permite testar a API REST
    public IActionResult Api() => View();

    [Route("Home/Erro/{codigo?}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public IActionResult Erro(int? codigo)
    {
        ViewBag.Codigo = codigo ?? 500;
        ViewBag.Rastreio = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View();
    }
}
