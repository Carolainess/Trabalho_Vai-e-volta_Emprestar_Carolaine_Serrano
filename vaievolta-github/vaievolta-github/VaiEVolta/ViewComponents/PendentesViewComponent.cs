using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaiEVolta.Data;
using VaiEVolta.Helpers;
using VaiEVolta.Models;

namespace VaiEVolta.ViewComponents;

// Bolinha no menu com quantos pedidos estão esperando a resposta da usuária
public class PendentesViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    public PendentesViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var id = UserClaimsPrincipal.Id();
        var total = await _db.Emprestimos.CountAsync(e =>
            e.Status == StatusEmprestimo.AguardandoAprovacao && e.Objeto!.DonoId == id);
        return View(total);
    }
}
