using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaiEVolta.Data;
using VaiEVolta.Helpers;
using VaiEVolta.Models;
using VaiEVolta.Models.ViewModels;

namespace VaiEVolta.Controllers;

// Rota protegida: só quem tem o perfil Administrador entra
[Authorize(Roles = nameof(Perfil.Administrador))]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var porUsuario = await _db.Objetos.GroupBy(o => o.DonoId)
            .Select(g => new { g.Key, Total = g.Count() }).ToListAsync();

        return View(new AdminViewModel
        {
            TotalUsuarios = await _db.Usuarios.CountAsync(),
            TotalObjetos = await _db.Objetos.CountAsync(),
            Emprestados = await _db.Emprestimos.CountAsync(e => e.Status == StatusEmprestimo.Emprestado),
            Aguardando = await _db.Emprestimos.CountAsync(e => e.Status == StatusEmprestimo.AguardandoAprovacao),
            Devolvidos = await _db.Emprestimos.CountAsync(e => e.Status == StatusEmprestimo.Devolvido),
            Usuarios = await _db.Usuarios.OrderBy(u => u.Nome).ToListAsync(),
            ObjetosPorUsuario = porUsuario.ToDictionary(x => x.Key, x => x.Total),
            Emprestimos = await _db.Emprestimos
                .Include(e => e.Objeto).ThenInclude(o => o!.Dono)
                .Include(e => e.Solicitante)
                .OrderByDescending(e => e.DataSolicitacao)
                .Take(30)
                .ToListAsync()
        });
    }

    [HttpPost]
    public async Task<IActionResult> AlterarPerfil(int id)
    {
        if (id == User.Id())
        {
            TempData["Erro"] = "Você não pode alterar o seu próprio perfil.";
            return RedirectToAction(nameof(Index));
        }
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();

        usuario.Perfil = usuario.Perfil == Perfil.Administrador ? Perfil.Usuario : Perfil.Administrador;
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = $"{usuario.Nome} agora é {(usuario.Perfil == Perfil.Administrador ? "administrador" : "usuário comum")}. A mudança vale no próximo login.";
        return RedirectToAction(nameof(Index));
    }
}
