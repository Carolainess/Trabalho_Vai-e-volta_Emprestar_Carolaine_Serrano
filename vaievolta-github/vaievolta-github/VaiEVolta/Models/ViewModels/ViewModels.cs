using System.ComponentModel.DataAnnotations;

namespace VaiEVolta.Models.ViewModels;

public class CadastroViewModel
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome precisa ter pelo menos 3 letras.")]
    [Display(Name = "Nome completo")]
    public string Nome { get; set; } = "";

    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = "";

    [StringLength(100, ErrorMessage = "Curso muito longo.")]
    [Display(Name = "Curso")]
    public string? Curso { get; set; }

    [StringLength(20, ErrorMessage = "Telefone muito longo.")]
    [Display(Name = "WhatsApp")]
    public string? Telefone { get; set; }

    [Required(ErrorMessage = "Crie uma senha.")]
    [StringLength(50, MinimumLength = 6, ErrorMessage = "A senha precisa ter pelo menos 6 caracteres.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = "";

    [Required(ErrorMessage = "Confirme a senha.")]
    [Compare(nameof(Senha), ErrorMessage = "As senhas não são iguais.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar senha")]
    public string ConfirmarSenha { get; set; } = "";
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Informe sua senha.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class PerfilViewModel
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome precisa ter pelo menos 3 letras.")]
    [Display(Name = "Nome completo")]
    public string Nome { get; set; } = "";

    public string Email { get; set; } = "";

    [StringLength(100, ErrorMessage = "Curso muito longo.")]
    public string? Curso { get; set; }

    [StringLength(20, ErrorMessage = "Telefone muito longo.")]
    [Display(Name = "WhatsApp")]
    public string? Telefone { get; set; }

    public int TotalObjetos { get; set; }
    public int TotalEmprestados { get; set; }
    public int TotalPedidos { get; set; }
    public DateTime MembroDesde { get; set; }
    public bool Admin { get; set; }
}

public class ObjetoFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Dê um nome ao objeto.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Use entre 3 e 100 caracteres.")]
    public string Nome { get; set; } = "";

    [Required(ErrorMessage = "Conte um pouco sobre o objeto.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Escreva pelo menos 10 caracteres.")]
    [Display(Name = "Descrição")]
    public string Descricao { get; set; } = "";

    [Required(ErrorMessage = "Escolha uma categoria.")]
    public Categoria? Categoria { get; set; }

    [Display(Name = "Disponível para empréstimo")]
    public bool Disponivel { get; set; } = true;

    [Display(Name = "Fotografia")]
    public IFormFile? Foto { get; set; }

    public string? FotoAtual { get; set; }
    public bool Emprestado { get; set; }
}

public class CatalogoViewModel
{
    public List<Objeto> Objetos { get; set; } = [];
    public Dictionary<int, string> Situacoes { get; set; } = [];
    public string? Busca { get; set; }
    public Categoria? Categoria { get; set; }
    public bool SoDisponiveis { get; set; }
}

public class DetalhesViewModel
{
    public Objeto Objeto { get; set; } = null!;
    public string Situacao { get; set; } = "";
    public Emprestimo? MeuPedido { get; set; }
    public List<Emprestimo> Pedidos { get; set; } = [];
    public int VezesEmprestado { get; set; }

    [Required(ErrorMessage = "Escolha até quando vai ficar com o objeto.")]
    [Display(Name = "Devolvo até")]
    public DateOnly? DataPrevistaDevolucao { get; set; }

    [StringLength(500, ErrorMessage = "Mensagem muito longa.")]
    public string? Mensagem { get; set; }
}

public class SolicitarViewModel
{
    public int ObjetoId { get; set; }

    [Required(ErrorMessage = "Escolha até quando vai ficar com o objeto.")]
    public DateOnly? DataPrevistaDevolucao { get; set; }

    [StringLength(500, ErrorMessage = "Mensagem muito longa.")]
    public string? Mensagem { get; set; }
}

public class PainelEmprestimosViewModel
{
    public List<Emprestimo> Recebidos { get; set; } = [];
    public List<Emprestimo> Feitos { get; set; } = [];
}

public class AdminViewModel
{
    public int TotalUsuarios { get; set; }
    public int TotalObjetos { get; set; }
    public int Emprestados { get; set; }
    public int Aguardando { get; set; }
    public int Devolvidos { get; set; }
    public List<Usuario> Usuarios { get; set; } = [];
    public List<Emprestimo> Emprestimos { get; set; } = [];
    public Dictionary<int, int> ObjetosPorUsuario { get; set; } = [];
}
