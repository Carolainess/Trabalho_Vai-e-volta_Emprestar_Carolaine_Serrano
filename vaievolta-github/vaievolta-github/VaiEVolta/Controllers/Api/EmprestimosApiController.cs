using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaiEVolta.Data;
using VaiEVolta.Helpers;
using VaiEVolta.Models;
using VaiEVolta.Models.ViewModels;
using VaiEVolta.Services;

namespace VaiEVolta.Controllers.Api;

/// <summary>
/// API REST de empréstimos: solicitar, consultar, aprovar, recusar, devolver e cancelar.
/// Usa as mesmas regras (EmprestimoService) das telas do site.
/// </summary>
[ApiController]
[Route("api/emprestimos")]
[Authorize]
[IgnoreAntiforgeryToken]
public class EmprestimosApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmprestimoService _servico;

    public EmprestimosApiController(AppDbContext db, EmprestimoService servico)
    {
        _db = db;
        _servico = servico;
    }

    // GET /api/emprestimos?status=Emprestado
    // Usuário comum vê os empréstimos em que participa; o administrador vê todos.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmprestimoDto>>> Listar(StatusEmprestimo? status)
    {
        var eu = User.Id();
        IQueryable<Emprestimo> consulta = _db.Emprestimos
            .Include(e => e.Objeto).ThenInclude(o => o!.Dono)
            .Include(e => e.Solicitante);

        if (!User.EhAdmin())
            consulta = consulta.Where(e => e.SolicitanteId == eu || e.Objeto!.DonoId == eu);
        if (status.HasValue)
            consulta = consulta.Where(e => e.Status == status);

        var lista = await consulta.OrderByDescending(e => e.DataSolicitacao).ToListAsync();
        return Ok(lista.Select(EmprestimoDto.De));
    }

    // GET /api/emprestimos/3
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmprestimoDto>> Buscar(int id)
    {
        var e = await _db.Emprestimos
            .Include(x => x.Objeto).ThenInclude(o => o!.Dono)
            .Include(x => x.Solicitante)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return NotFound(new { erro = "Empréstimo não encontrado." });

        var eu = User.Id();
        if (e.SolicitanteId != eu && e.Objeto!.DonoId != eu && !User.EhAdmin()) return Forbid();
        return EmprestimoDto.De(e);
    }

    // POST /api/emprestimos  { "objetoId": 1, "dataPrevistaDevolucao": "2026-11-20", "mensagem": "..." }
    [HttpPost]
    public async Task<ActionResult<EmprestimoDto>> Solicitar(SolicitacaoDto dto)
    {
        var r = await _servico.SolicitarAsync(dto.ObjetoId!.Value, User.Id(), dto.DataPrevistaDevolucao!.Value, dto.Mensagem);
        if (!r.Sucesso) return BadRequest(new { erro = r.Mensagem });
        return CreatedAtAction(nameof(Buscar), new { id = r.Emprestimo!.Id }, await CarregarDtoAsync(r.Emprestimo.Id));
    }

    // PUT /api/emprestimos/3/aprovar
    [HttpPut("{id:int}/aprovar")]
    public async Task<IActionResult> Aprovar(int id) => await Responder(await _servico.AprovarAsync(id, User.Id(), User.EhAdmin()));

    // PUT /api/emprestimos/3/recusar
    [HttpPut("{id:int}/recusar")]
    public async Task<IActionResult> Recusar(int id) => await Responder(await _servico.RecusarAsync(id, User.Id(), User.EhAdmin()));

    // PUT /api/emprestimos/3/devolver
    [HttpPut("{id:int}/devolver")]
    public async Task<IActionResult> Devolver(int id) => await Responder(await _servico.DevolverAsync(id, User.Id(), User.EhAdmin()));

    // DELETE /api/emprestimos/3  (cancela um pedido que ainda está aguardando)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancelar(int id)
    {
        var r = await _servico.CancelarAsync(id, User.Id());
        return r.Sucesso ? NoContent() : BadRequest(new { erro = r.Mensagem });
    }

    private async Task<IActionResult> Responder(Resultado r) =>
        r.Sucesso
            ? Ok(new { mensagem = r.Mensagem, emprestimo = await CarregarDtoAsync(r.Emprestimo!.Id) })
            : BadRequest(new { erro = r.Mensagem });

    private async Task<EmprestimoDto> CarregarDtoAsync(int id) =>
        EmprestimoDto.De(await _db.Emprestimos
            .Include(e => e.Objeto).ThenInclude(o => o!.Dono)
            .Include(e => e.Solicitante)
            .FirstAsync(e => e.Id == id));
}
