using System.ComponentModel.DataAnnotations;

namespace VaiEVolta.Models;

public enum Categoria
{
    [Display(Name = "Livros")] Livros = 0,
    [Display(Name = "Eletrônicos")] Eletronicos = 1,
    [Display(Name = "Material de estudo")] MaterialDeEstudo = 2,
    [Display(Name = "Laboratório")] Laboratorio = 3,
    [Display(Name = "Esporte e lazer")] EsporteELazer = 4,
    [Display(Name = "Outros")] Outros = 5
}

public class Objeto
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nome { get; set; } = "";

    [Required, MaxLength(1000)]
    public string Descricao { get; set; } = "";

    public Categoria Categoria { get; set; }

    // Caminho da foto (ex.: /uploads/abc.jpg)
    [MaxLength(300)]
    public string? FotoUrl { get; set; }

    // Fica falso enquanto está emprestado ou quando o dono pausa o anúncio
    public bool Disponivel { get; set; } = true;

    public int DonoId { get; set; }
    public Usuario? Dono { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
