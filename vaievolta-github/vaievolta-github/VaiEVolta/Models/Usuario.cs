using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VaiEVolta.Models;

public enum Perfil
{
    Usuario = 0,
    Administrador = 1
}

// E-mail não pode repetir: o banco cria um índice único
[Index(nameof(Email), IsUnique = true)]
public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nome { get; set; } = "";

    [Required, MaxLength(150)]
    public string Email { get; set; } = "";

    [Required]
    public string SenhaHash { get; set; } = "";

    [MaxLength(100)]
    public string? Curso { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    public Perfil Perfil { get; set; } = Perfil.Usuario;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public string Iniciais =>
        string.Concat(Nome.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                          .Take(2).Select(p => char.ToUpper(p[0])));
}
