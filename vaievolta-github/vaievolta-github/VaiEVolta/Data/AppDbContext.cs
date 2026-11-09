using Microsoft.EntityFrameworkCore;
using VaiEVolta.Models;

namespace VaiEVolta.Data;

// Ponte entre o C# e o PostgreSQL: cada DbSet vira uma tabela
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Objeto> Objetos => Set<Objeto>();
    public DbSet<Emprestimo> Emprestimos => Set<Emprestimo>();
}
