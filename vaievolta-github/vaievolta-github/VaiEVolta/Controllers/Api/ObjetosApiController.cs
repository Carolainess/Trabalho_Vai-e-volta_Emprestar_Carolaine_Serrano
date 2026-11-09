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
/// API REST de objetos: CRUD completo (GET, POST, PUT, DELETE) respondendo em JSON.
/// </summary>
[ApiController]
[Route("api/objetos")]
[IgnoreAntiforgeryToken] // a API é usada por outros programas (Postman, apps), não por formulários
public class ObjetosApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmprestimoService _emprestimos;

    public ObjetosApiController(AppDbContext db, EmprestimoService emprestimos)
    {
        _db = db;
        _emprestimos = emprestimos;
    }

    // GET /api/objetos?busca=livro&categoria=Livros&disponiveis=true
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ObjetoDto>>> Listar(string? busca, Categoria? categoria, bool disponiveis = false)
    {
        IQueryable<Objeto> consulta = _db.Objetos.Include(o => o.Dono);
        if (!string.IsNullOrWhiteSpace(busca))
            consulta = consulta.Where(o => EF.Functions.ILike(o.Nome, $"%{busca.Trim()}%"));
        if (categoria.HasValue)
            consulta = consulta.Where(o => o.Categoria == categoria);
        if (disponiveis)
            consulta = consulta.Where(o => o.Disponivel);

        var objetos = await consulta.OrderByDescending(o => o.CriadoEm).ToListAsync();
        var situacoes = await _emprestimos.SituacoesAsync();
        return Ok(objetos.Select(o => ObjetoDto.De(o, situacoes.GetValueOrDefault(o.Id, "Disponível"))));
    }

    // GET /api/objetos/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ObjetoDto>> Buscar(int id)
    {
        var o = await _db.Objetos.Include(x => x.Dono).FirstOrDefaultAsync(x => x.Id == id);
        if (o is null) return NotFound(new { erro = "Objeto não encontrado." });
        var situacoes = await _emprestimos.SituacoesAsync();
        return ObjetoDto.De(o, situacoes.GetValueOrDefault(id, "Disponível"));
    }

    // POST /api/objetos  (precisa estar logado)
    [HttpPost, Authorize]
    public async Task<ActionResult<ObjetoDto>> Criar(ObjetoEntradaDto dto)
    {
        var objeto = new Objeto
        {
            Nome = dto.Nome.Trim(),
            Descricao = dto.Descricao.Trim(),
            Categoria = dto.Categoria!.Value,
            Disponivel = dto.Disponivel,
            FotoUrl = dto.FotoUrl,
            DonoId = User.Id()
        };
        _db.Objetos.Add(objeto);
        await _db.SaveChangesAsync();
        objeto.Dono = await _db.Usuarios.FindAsync(objeto.DonoId);

        return CreatedAtAction(nameof(Buscar), new { id = objeto.Id }, ObjetoDto.De(objeto, objeto.Disponivel ? "Disponível" : "Indisponível"));
    }

    // PUT /api/objetos/5  (só o dono ou o administrador)
    [HttpPut("{id:int}"), Authorize]
    public async Task<ActionResult<ObjetoDto>> Atualizar(int id, ObjetoEntradaDto dto)
    {
        var objeto = await _db.Objetos.Include(o => o.Dono).FirstOrDefaultAsync(o => o.Id == id);
        if (objeto is null) return NotFound(new { erro = "Objeto não encontrado." });
        if (objeto.DonoId != User.Id() && !User.EhAdmin()) return Forbid();

        var emprestado = await _db.Emprestimos.AnyAsync(e => e.ObjetoId == id && e.Status == StatusEmprestimo.Emprestado);

        objeto.Nome = dto.Nome.Trim();
        objeto.Descricao = dto.Descricao.Trim();
        objeto.Categoria = dto.Categoria!.Value;
        if (dto.FotoUrl is not null) objeto.FotoUrl = dto.FotoUrl;
        if (!emprestado) objeto.Disponivel = dto.Disponivel;
        await _db.SaveChangesAsync();

        var situacoes = await _emprestimos.SituacoesAsync();
        return ObjetoDto.De(objeto, situacoes.GetValueOrDefault(id, "Disponível"));
    }

    // DELETE /api/objetos/5  (só o dono ou o administrador)
    [HttpDelete("{id:int}"), Authorize]
    public async Task<IActionResult> Excluir(int id)
    {
        var objeto = await _db.Objetos.FindAsync(id);
        if (objeto is null) return NotFound(new { erro = "Objeto não encontrado." });
        if (objeto.DonoId != User.Id() && !User.EhAdmin()) return Forbid();
        if (await _db.Emprestimos.AnyAsync(e => e.ObjetoId == id && e.Status == StatusEmprestimo.Emprestado))
            return Conflict(new { erro = "O objeto está emprestado e não pode ser excluído." });

        _db.Objetos.Remove(objeto);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
