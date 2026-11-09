using Microsoft.EntityFrameworkCore;
using VaiEVolta.Data;
using VaiEVolta.Models;

namespace VaiEVolta.Services;

public record Resultado(bool Sucesso, string Mensagem, Emprestimo? Emprestimo = null)
{
    public static Resultado Ok(string msg, Emprestimo? e = null) => new(true, msg, e);
    public static Resultado Erro(string msg) => new(false, msg);
}

// Regras de empréstimo num lugar só: usadas pelas telas (MVC) e pela API REST
public class EmprestimoService
{
    private readonly AppDbContext _db;

    public EmprestimoService(AppDbContext db) => _db = db;

    public async Task<Resultado> SolicitarAsync(int objetoId, int usuarioId, DateOnly devolucao, string? mensagem)
    {
        var objeto = await _db.Objetos.FirstOrDefaultAsync(o => o.Id == objetoId);
        if (objeto is null) return Resultado.Erro("Objeto não encontrado.");
        if (objeto.DonoId == usuarioId) return Resultado.Erro("Você não pode pedir emprestado um objeto seu.");
        if (!objeto.Disponivel) return Resultado.Erro("Este objeto não está disponível no momento.");
        if (devolucao < DateOnly.FromDateTime(DateTime.Now))
            return Resultado.Erro("A data de devolução não pode estar no passado.");

        var jaPediu = await _db.Emprestimos.AnyAsync(e =>
            e.ObjetoId == objetoId && e.SolicitanteId == usuarioId &&
            e.Status == StatusEmprestimo.AguardandoAprovacao);
        if (jaPediu) return Resultado.Erro("Você já tem uma solicitação aguardando para este objeto.");

        var emprestimo = new Emprestimo
        {
            ObjetoId = objetoId,
            SolicitanteId = usuarioId,
            DataPrevistaDevolucao = devolucao,
            Mensagem = string.IsNullOrWhiteSpace(mensagem) ? null : mensagem.Trim()
        };
        _db.Emprestimos.Add(emprestimo);
        await _db.SaveChangesAsync();
        return Resultado.Ok("Solicitação enviada! Agora é só aguardar a aprovação do dono.", emprestimo);
    }

    public async Task<Resultado> AprovarAsync(int id, int usuarioId, bool admin)
    {
        var e = await BuscarAsync(id);
        if (e is null) return Resultado.Erro("Solicitação não encontrada.");
        if (!admin && e.Objeto!.DonoId != usuarioId) return Resultado.Erro("Só o dono do objeto pode aprovar.");
        if (e.Status != StatusEmprestimo.AguardandoAprovacao) return Resultado.Erro("Essa solicitação já foi respondida.");
        if (!e.Objeto!.Disponivel) return Resultado.Erro("O objeto já está emprestado para outra pessoa.");

        e.Status = StatusEmprestimo.Emprestado;
        e.DataResposta = DateTime.UtcNow;
        e.Objeto.Disponivel = false; // atualiza a disponibilidade automaticamente

        // Os outros pedidos pelo mesmo objeto são recusados automaticamente
        var outros = await _db.Emprestimos
            .Where(x => x.ObjetoId == e.ObjetoId && x.Id != e.Id && x.Status == StatusEmprestimo.AguardandoAprovacao)
            .ToListAsync();
        foreach (var o in outros)
        {
            o.Status = StatusEmprestimo.Recusado;
            o.DataResposta = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Resultado.Ok($"Empréstimo aprovado! {e.Objeto.Nome} agora está com {e.Solicitante!.Nome}.", e);
    }

    public async Task<Resultado> RecusarAsync(int id, int usuarioId, bool admin)
    {
        var e = await BuscarAsync(id);
        if (e is null) return Resultado.Erro("Solicitação não encontrada.");
        if (!admin && e.Objeto!.DonoId != usuarioId) return Resultado.Erro("Só o dono do objeto pode recusar.");
        if (e.Status != StatusEmprestimo.AguardandoAprovacao) return Resultado.Erro("Essa solicitação já foi respondida.");

        e.Status = StatusEmprestimo.Recusado;
        e.DataResposta = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Resultado.Ok("Solicitação recusada.", e);
    }

    // O dono confirma que recebeu o objeto de volta
    public async Task<Resultado> DevolverAsync(int id, int usuarioId, bool admin)
    {
        var e = await BuscarAsync(id);
        if (e is null) return Resultado.Erro("Empréstimo não encontrado.");
        if (!admin && e.Objeto!.DonoId != usuarioId) return Resultado.Erro("Só o dono do objeto pode confirmar a devolução.");
        if (e.Status != StatusEmprestimo.Emprestado) return Resultado.Erro("Esse objeto não está emprestado.");

        e.Status = StatusEmprestimo.Devolvido;
        e.DataDevolucao = DateTime.UtcNow;
        e.Objeto!.Disponivel = true; // volta para o catálogo
        await _db.SaveChangesAsync();
        return Resultado.Ok($"Devolução confirmada! {e.Objeto.Nome} está disponível de novo.", e);
    }

    // Quem pediu pode desistir enquanto ainda não foi respondido
    public async Task<Resultado> CancelarAsync(int id, int usuarioId)
    {
        var e = await BuscarAsync(id);
        if (e is null) return Resultado.Erro("Solicitação não encontrada.");
        if (e.SolicitanteId != usuarioId) return Resultado.Erro("Só quem fez o pedido pode cancelar.");
        if (e.Status != StatusEmprestimo.AguardandoAprovacao) return Resultado.Erro("Essa solicitação já foi respondida.");

        _db.Emprestimos.Remove(e);
        await _db.SaveChangesAsync();
        return Resultado.Ok("Solicitação cancelada.");
    }

    // Situação que aparece no card: Disponível, Aguardando aprovação, Emprestado ou Indisponível
    public async Task<Dictionary<int, string>> SituacoesAsync()
    {
        var emprestados = await _db.Emprestimos
            .Where(e => e.Status == StatusEmprestimo.Emprestado).Select(e => e.ObjetoId).ToListAsync();
        var aguardando = await _db.Emprestimos
            .Where(e => e.Status == StatusEmprestimo.AguardandoAprovacao).Select(e => e.ObjetoId).ToListAsync();
        var objetos = await _db.Objetos.Select(o => new { o.Id, o.Disponivel }).ToListAsync();

        return objetos.ToDictionary(o => o.Id, o =>
            emprestados.Contains(o.Id) ? "Emprestado" :
            !o.Disponivel ? "Indisponível" :
            aguardando.Contains(o.Id) ? "Aguardando aprovação" : "Disponível");
    }

    private Task<Emprestimo?> BuscarAsync(int id) =>
        _db.Emprestimos
            .Include(e => e.Objeto)
            .Include(e => e.Solicitante)
            .FirstOrDefaultAsync(e => e.Id == id);
}
