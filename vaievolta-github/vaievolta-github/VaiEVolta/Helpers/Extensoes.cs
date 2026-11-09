using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using VaiEVolta.Models;

namespace VaiEVolta.Helpers;

public static class Extensoes
{
    // Pega o nome bonito do enum: Categoria.MaterialDeEstudo -> "Material de estudo"
    public static string Nome(this Enum valor) =>
        valor.GetType().GetMember(valor.ToString()).FirstOrDefault()
             ?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? valor.ToString();

    public static string Icone(this Categoria c) => c switch
    {
        Categoria.Livros => "bi-book",
        Categoria.Eletronicos => "bi-cpu",
        Categoria.MaterialDeEstudo => "bi-pencil-square",
        Categoria.Laboratorio => "bi-eyedropper",
        Categoria.EsporteELazer => "bi-dribbble",
        _ => "bi-box-seam"
    };

    // Classe CSS de cada situação (cores do selo)
    public static string ClasseSituacao(string situacao) => situacao switch
    {
        "Disponível" => "selo-disponivel",
        "Aguardando aprovação" => "selo-aguardando",
        "Emprestado" => "selo-emprestado",
        "Devolvido" => "selo-devolvido",
        _ => "selo-neutro"
    };

    public static string Classe(this StatusEmprestimo s) => s switch
    {
        StatusEmprestimo.AguardandoAprovacao => "selo-aguardando",
        StatusEmprestimo.Emprestado => "selo-emprestado",
        StatusEmprestimo.Devolvido => "selo-devolvido",
        _ => "selo-recusado"
    };

    public static int Id(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public static bool EhAdmin(this ClaimsPrincipal user) => user.IsInRole(nameof(Perfil.Administrador));

    public static string DataBr(this DateTime utc) => utc.ToLocalTime().ToString("dd/MM/yyyy");
    public static string DataHoraBr(this DateTime utc) => utc.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm");
    public static string DataBr(this DateOnly d) => d.ToString("dd/MM/yyyy");

    public static string Tempo(this DateTime utc)
    {
        var t = DateTime.UtcNow - utc;
        if (t.TotalMinutes < 1) return "agora mesmo";
        if (t.TotalHours < 1) return $"há {(int)t.TotalMinutes} min";
        if (t.TotalDays < 1) return $"há {(int)t.TotalHours} h";
        if (t.TotalDays < 2) return "ontem";
        return $"há {(int)t.TotalDays} dias";
    }
}
