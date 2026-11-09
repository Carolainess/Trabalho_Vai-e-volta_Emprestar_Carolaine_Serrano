using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaiEVolta.Data;
using VaiEVolta.Helpers;
using VaiEVolta.Models;
using VaiEVolta.Models.ViewModels;
using VaiEVolta.Services;

namespace VaiEVolta.Controllers;

public class ObjetosController : Controller
{
    private readonly AppDbContext _db;
    private readonly EmprestimoService _emprestimos;
    private readonly FotoService _fotos;

    public ObjetosController(AppDbContext db, EmprestimoService emprestimos, FotoService fotos)
    {
        _db = db;
        _emprestimos = emprestimos;
        _fotos = fotos;
    }

    // ---------- Catálogo (aberto para todos) ----------
    public async Task<IActionResult> Index(string? busca, Categoria? categoria, bool disponiveis = false)
    {
        IQueryable<Objeto> consulta = _db.Objetos.Include(o => o.Dono);

        if (!string.IsNullOrWhiteSpace(busca))
            consulta = consulta.Where(o => EF.Functions.ILike(o.Nome, $"%{busca.Trim()}%")); // ignora maiúsculas/minúsculas
        if (categoria.HasValue)
            consulta = consulta.Where(o => o.Categoria == categoria);
        if (disponiveis)
            consulta = consulta.Where(o => o.Disponivel);

        return View(new CatalogoViewModel
        {
            Objetos = await consulta.OrderByDescending(o => o.Disponivel).ThenByDescending(o => o.CriadoEm).ToListAsync(),
            Situacoes = await _emprestimos.SituacoesAsync(),
            Busca = busca,
            Categoria = categoria,
            SoDisponiveis = disponiveis
        });
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        var objeto = await _db.Objetos.Include(o => o.Dono).FirstOrDefaultAsync(o => o.Id == id);
        if (objeto is null) return NotFound();

        var situacoes = await _emprestimos.SituacoesAsync();
        var vm = new DetalhesViewModel
        {
            Objeto = objeto,
            Situacao = situacoes.GetValueOrDefault(id, "Disponível"),
            DataPrevistaDevolucao = DateOnly.FromDateTime(DateTime.Now.AddDays(3)),
            VezesEmprestado = await _db.Emprestimos.CountAsync(e => e.ObjetoId == id &&
                (e.Status == StatusEmprestimo.Emprestado || e.Status == StatusEmprestimo.Devolvido))
        };

        if (User.Identity?.IsAuthenticated == true)
        {
            var eu = User.Id();
            vm.MeuPedido = await _db.Emprestimos
                .Where(e => e.ObjetoId == id && e.SolicitanteId == eu &&
                            (e.Status == StatusEmprestimo.AguardandoAprovacao || e.Status == StatusEmprestimo.Emprestado))
                .FirstOrDefaultAsync();

            // O dono (ou o admin) vê os pedidos em aberto ali mesmo
            if (objeto.DonoId == eu || User.EhAdmin())
                vm.Pedidos = await _db.Emprestimos
                    .Include(e => e.Solicitante)
                    .Where(e => e.ObjetoId == id &&
                                (e.Status == StatusEmprestimo.AguardandoAprovacao || e.Status == StatusEmprestimo.Emprestado))
                    .OrderBy(e => e.DataSolicitacao)
                    .ToListAsync();
        }
        return View(vm);
    }

    // ---------- Meus objetos ----------
    [Authorize]
    public async Task<IActionResult> Meus()
    {
        var eu = User.Id();
        var objetos = await _db.Objetos.Include(o => o.Dono)
            .Where(o => o.DonoId == eu).OrderByDescending(o => o.CriadoEm).ToListAsync();
        ViewBag.Situacoes = await _emprestimos.SituacoesAsync();
        return View(objetos);
    }

    // ---------- Cadastrar ----------
    [Authorize]
    public IActionResult Criar() => View("Formulario", new ObjetoFormViewModel());

    [HttpPost, Authorize]
    public async Task<IActionResult> Criar(ObjetoFormViewModel vm)
    {
        ValidarFoto(vm);
        if (!ModelState.IsValid) return View("Formulario", vm);

        var objeto = new Objeto
        {
            Nome = vm.Nome.Trim(),
            Descricao = vm.Descricao.Trim(),
            Categoria = vm.Categoria!.Value,
            Disponivel = vm.Disponivel,
            DonoId = User.Id(),
            FotoUrl = vm.Foto is { Length: > 0 } ? await _fotos.SalvarAsync(vm.Foto) : null
        };
        _db.Objetos.Add(objeto);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = "Objeto anunciado! Ele já aparece no catálogo.";
        return RedirectToAction(nameof(Detalhes), new { id = objeto.Id });
    }

    // ---------- Editar ----------
    [Authorize]
    public async Task<IActionResult> Editar(int id)
    {
        var objeto = await _db.Objetos.FindAsync(id);
        if (objeto is null) return NotFound();
        if (!PodeAlterar(objeto)) return Forbid();

        return View("Formulario", new ObjetoFormViewModel
        {
            Id = objeto.Id,
            Nome = objeto.Nome,
            Descricao = objeto.Descricao,
            Categoria = objeto.Categoria,
            Disponivel = objeto.Disponivel,
            FotoAtual = objeto.FotoUrl,
            Emprestado = await EstaEmprestadoAsync(id)
        });
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Editar(int id, ObjetoFormViewModel vm)
    {
        var objeto = await _db.Objetos.FindAsync(id);
        if (objeto is null) return NotFound();
        if (!PodeAlterar(objeto)) return Forbid();

        var emprestado = await EstaEmprestadoAsync(id);
        ValidarFoto(vm);
        if (!ModelState.IsValid)
        {
            vm.Id = id; vm.FotoAtual = objeto.FotoUrl; vm.Emprestado = emprestado;
            return View("Formulario", vm);
        }

        objeto.Nome = vm.Nome.Trim();
        objeto.Descricao = vm.Descricao.Trim();
        objeto.Categoria = vm.Categoria!.Value;
        // Enquanto está emprestado a disponibilidade é controlada pelo sistema
        if (!emprestado) objeto.Disponivel = vm.Disponivel;

        if (vm.Foto is { Length: > 0 })
        {
            _fotos.Apagar(objeto.FotoUrl);
            objeto.FotoUrl = await _fotos.SalvarAsync(vm.Foto);
        }
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = "Alterações salvas!";
        return RedirectToAction(nameof(Detalhes), new { id });
    }

    // ---------- Excluir ----------
    [HttpPost, Authorize]
    public async Task<IActionResult> Excluir(int id)
    {
        var objeto = await _db.Objetos.FindAsync(id);
        if (objeto is null) return NotFound();
        if (!PodeAlterar(objeto)) return Forbid();

        if (await EstaEmprestadoAsync(id))
        {
            TempData["Erro"] = "Não dá para excluir: o objeto está emprestado. Confirme a devolução primeiro.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        _fotos.Apagar(objeto.FotoUrl);
        _db.Objetos.Remove(objeto); // o histórico de empréstimos dele é removido junto (cascata)
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"\"{objeto.Nome}\" foi removido.";
        return RedirectToAction(nameof(Meus));
    }

    // ---------- Auxiliares ----------
    private bool PodeAlterar(Objeto o) => o.DonoId == User.Id() || User.EhAdmin();

    private Task<bool> EstaEmprestadoAsync(int id) =>
        _db.Emprestimos.AnyAsync(e => e.ObjetoId == id && e.Status == StatusEmprestimo.Emprestado);

    private void ValidarFoto(ObjetoFormViewModel vm)
    {
        var erro = _fotos.Validar(vm.Foto);
        if (erro is not null) ModelState.AddModelError(nameof(vm.Foto), erro);
    }
}
