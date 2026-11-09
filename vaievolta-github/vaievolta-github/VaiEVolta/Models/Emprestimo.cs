using System.ComponentModel.DataAnnotations;

namespace VaiEVolta.Models;

public enum StatusEmprestimo
{
    [Display(Name = "Aguardando aprovação")] AguardandoAprovacao = 0,
    [Display(Name = "Emprestado")] Emprestado = 1,
    [Display(Name = "Devolvido")] Devolvido = 2,
    [Display(Name = "Recusado")] Recusado = 3
}

public class Emprestimo
{
    public int Id { get; set; }

    public int ObjetoId { get; set; }
    public Objeto? Objeto { get; set; }

    // Quem pediu emprestado
    public int SolicitanteId { get; set; }
    public Usuario? Solicitante { get; set; }

    [MaxLength(500)]
    public string? Mensagem { get; set; }

    public DateTime DataSolicitacao { get; set; } = DateTime.UtcNow;

    // Até quando a pessoa pretende ficar com o objeto
    public DateOnly DataPrevistaDevolucao { get; set; }

    public DateTime? DataResposta { get; set; }
    public DateTime? DataDevolucao { get; set; }

    public StatusEmprestimo Status { get; set; } = StatusEmprestimo.AguardandoAprovacao;

    public bool Atrasado =>
        Status == StatusEmprestimo.Emprestado &&
        DataPrevistaDevolucao < DateOnly.FromDateTime(DateTime.Now);
}
