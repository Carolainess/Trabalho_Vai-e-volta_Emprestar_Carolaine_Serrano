using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaiEVolta.Data;
using VaiEVolta.Helpers;
using VaiEVolta.Models;
using VaiEVolta.Models.ViewModels;
using VaiEVolta.Services;

namespace VaiEVolta.Controllers;

[Authorize] // tudo aqui exige login
public class EmprestimosController : Controller
{
    private readonly AppDbContext _db;
    private readonly EmprestimoService _servico;

    public EmprestimosController(AppDbContext db, EmprestimoService servico)
    {
        _db = db;
        _servico = servico;
    }

    // Painel com duas abas: pedidos que recebi (sou dono) e pedidos que fiz
    public async Task<IActionResult> Index()
    {
        var eu = User.Id();
        var vm = new PainelEmprestimosViewModel
        {
            Recebidos = await _db.Emprestimos
                .Include(e => e.Objeto).Include(e => e.Solicitante)
                .Where(e => e.Objeto!.DonoId == eu)
                .OrderBy(e => e.Status).ThenByDescending(e => e.DataSolicitacao)
                .ToListAsync(),
            Feitos = await _db.Emprestimos
                .Include(e => e.Objeto).ThenInclude(o => o!.Dono)
                .Where(e => e.SolicitanteId == eu)
                .OrderBy(e => e.Status).ThenByDescending(e => e.DataSolicitacao)
                .ToListAsync()
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Solicitar(SolicitarViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Erro"] = ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage;
            return RedirectToAction("Detalhes", "Objetos", new { id = vm.ObjetoId });
        }
        var r = await _servico.SolicitarAsync(vm.ObjetoId, User.Id(), vm.DataPrevistaDevolucao!.Value, vm.Mensagem);
        Avisar(r);
        return RedirectToAction("Detalhes", "Objetos", new { id = vm.ObjetoId });
    }

    [HttpPost]
    public async Task<IActionResult> Aprovar(int id, string? voltar) =>
        Voltar(await _servico.AprovarAsync(id, User.Id(), User.EhAdmin()), voltar);

    [HttpPost]
    public async Task<IActionResult> Recusar(int id, string? voltar) =>
        Voltar(await _servico.RecusarAsync(id, User.Id(), User.EhAdmin()), voltar);

    [HttpPost]
    public async Task<IActionResult> Devolver(int id, string? voltar) =>
        Voltar(await _servico.DevolverAsync(id, User.Id(), User.EhAdmin()), voltar);

    [HttpPost]
    public async Task<IActionResult> Cancelar(int id, string? voltar) =>
        Voltar(await _servico.CancelarAsync(id, User.Id()), voltar);

    private void Avisar(Resultado r) => TempData[r.Sucesso ? "Sucesso" : "Erro"] = r.Mensagem;

    private IActionResult Voltar(Resultado r, string? voltar)
    {
        Avisar(r);
        return !string.IsNullOrEmpty(voltar) && Url.IsLocalUrl(voltar) ? Redirect(voltar) : RedirectToAction(nameof(Index));
    }
}
