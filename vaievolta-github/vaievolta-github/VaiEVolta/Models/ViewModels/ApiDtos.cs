using System.ComponentModel.DataAnnotations;

namespace VaiEVolta.Models.ViewModels;

// Formatos de entrada e saída da API REST (o que vai e volta em JSON)

public record PessoaDto(int Id, string Nome);

public record ObjetoDto(int Id, string Nome, string Descricao, Categoria Categoria, string? FotoUrl,
                        bool Disponivel, string Situacao, PessoaDto? Dono, DateTime CriadoEm)
{
    public static ObjetoDto De(Objeto o, string situacao) => new(o.Id, o.Nome, o.Descricao, o.Categoria, o.FotoUrl,
        o.Disponivel, situacao, o.Dono is null ? null : new PessoaDto(o.Dono.Id, o.Dono.Nome), o.CriadoEm);
}

public class ObjetoEntradaDto
{
    [Required(ErrorMessage = "O nome é obrigatório."), StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
    public string Nome { get; set; } = "";

    [Required(ErrorMessage = "A descrição é obrigatória."), StringLength(1000, MinimumLength = 10, ErrorMessage = "A descrição deve ter entre 10 e 1000 caracteres.")]
    public string Descricao { get; set; } = "";

    [Required(ErrorMessage = "A categoria é obrigatória.")]
    public Categoria? Categoria { get; set; }

    public bool Disponivel { get; set; } = true;

    [StringLength(300, ErrorMessage = "URL da foto muito longa.")]
    public string? FotoUrl { get; set; }
}

public record EmprestimoDto(int Id, int ObjetoId, string? Objeto, PessoaDto? Solicitante, PessoaDto? Dono,
                            StatusEmprestimo Status, string? Mensagem, DateTime DataSolicitacao,
                            DateOnly DataPrevistaDevolucao, DateTime? DataResposta, DateTime? DataDevolucao)
{
    public static EmprestimoDto De(Emprestimo e) => new(e.Id, e.ObjetoId, e.Objeto?.Nome,
        e.Solicitante is null ? null : new PessoaDto(e.Solicitante.Id, e.Solicitante.Nome),
        e.Objeto?.Dono is null ? null : new PessoaDto(e.Objeto.Dono.Id, e.Objeto.Dono.Nome),
        e.Status, e.Mensagem, e.DataSolicitacao, e.DataPrevistaDevolucao, e.DataResposta, e.DataDevolucao);
}

public class SolicitacaoDto
{
    [Required(ErrorMessage = "Informe o objeto.")]
    public int? ObjetoId { get; set; }

    [Required(ErrorMessage = "Informe a data prevista de devolução (aaaa-mm-dd).")]
    public DateOnly? DataPrevistaDevolucao { get; set; }

    [StringLength(500, ErrorMessage = "Mensagem muito longa.")]
    public string? Mensagem { get; set; }
}

public class LoginApiDto
{
    [Required(ErrorMessage = "Informe o e-mail."), EmailAddress(ErrorMessage = "E-mail inválido.")] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Informe a senha.")] public string Senha { get; set; } = "";
}
