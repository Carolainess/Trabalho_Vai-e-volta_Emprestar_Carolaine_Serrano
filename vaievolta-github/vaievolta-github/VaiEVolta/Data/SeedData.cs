using Microsoft.AspNetCore.Identity;
using VaiEVolta.Models;

namespace VaiEVolta.Data;

// Dados fictícios para a demonstração. Só roda quando o banco está vazio.
public static class SeedData
{
    public const string SenhaPadrao = "Senha@123";

    public static void Inicializar(AppDbContext db, IPasswordHasher<Usuario> hasher)
    {
        if (db.Usuarios.Any()) return;

        Usuario NovoUsuario(string nome, string email, string curso, string tel, Perfil perfil = Perfil.Usuario)
        {
            var u = new Usuario { Nome = nome, Email = email, Curso = curso, Telefone = tel, Perfil = perfil };
            u.SenhaHash = hasher.HashPassword(u, perfil == Perfil.Administrador ? "Admin@123" : SenhaPadrao);
            return u;
        }

        var admin = NovoUsuario("Administração FEMA", "admin@vaievolta.com", "Secretaria", "(18) 3302-1055", Perfil.Administrador);
        var carol = NovoUsuario("Carol Serrano", "carol@vaievolta.com", "Ciência da Computação", "(18) 99712-4410");
        var ana = NovoUsuario("Ana Beatriz Lima", "ana@vaievolta.com", "Ciência da Computação", "(18) 99635-2287");
        var bruna = NovoUsuario("Bruna Martins", "bruna@vaievolta.com", "Biomedicina", "(18) 99104-7731");
        var lucas = NovoUsuario("Lucas Andrade", "lucas@vaievolta.com", "Engenharia Química", "(18) 99820-3364");
        db.Usuarios.AddRange(admin, carol, ana, bruna, lucas);
        db.SaveChanges();

        Objeto NovoObjeto(string nome, string desc, Categoria cat, string foto, Usuario dono, int diasAtras) => new()
        {
            Nome = nome, Descricao = desc, Categoria = cat, FotoUrl = $"/img/objetos/{foto}.svg",
            DonoId = dono.Id, CriadoEm = DateTime.UtcNow.AddDays(-diasAtras)
        };

        var calculadora = NovoObjeto("Calculadora científica Casio fx-82",
            "Calculadora científica com 252 funções. Perfeita para Cálculo, Física e Estatística. Vai com a capinha de proteção.",
            Categoria.MaterialDeEstudo, "calculadora", carol, 20);
        var algoritmos = NovoObjeto("Livro Algoritmos: Teoria e Prática",
            "O famoso livro do Cormen, 3ª edição. Tem algumas anotações a lápis nos capítulos de grafos que ajudam bastante.",
            Categoria.Livros, "livro-algoritmos", ana, 18);
        var jaleco = NovoObjeto("Jaleco branco tamanho M",
            "Jaleco de algodão, manga longa, lavado e passado. Ideal para quem esqueceu o seu no dia da aula prática.",
            Categoria.Laboratorio, "jaleco", bruna, 15);
        var guardaChuva = NovoObjeto("Guarda-chuva grande",
            "Guarda-chuva resistente, cabe duas pessoas. Para aqueles dias em que a chuva pega de surpresa na saída da aula.",
            Categoria.Outros, "guarda-chuva", lucas, 12);
        var carregador = NovoObjeto("Carregador de notebook USB-C 65W",
            "Carregador universal USB-C, funciona na maioria dos notebooks recentes. Cabo de 1,8 m.",
            Categoria.Eletronicos, "carregador", lucas, 10);
        var arduino = NovoObjeto("Kit Arduino Uno com sensores",
            "Placa Arduino Uno, protoboard, jumpers, LEDs, resistores e sensores de temperatura e distância.",
            Categoria.Eletronicos, "arduino", carol, 9);
        var raquetes = NovoObjeto("Par de raquetes de ping-pong",
            "Duas raquetes e três bolinhas. Para o intervalo ou para o campeonato do centro acadêmico.",
            Categoria.EsporteELazer, "raquetes", ana, 7);
        var fone = NovoObjeto("Fone de ouvido Bluetooth",
            "Fone com cancelamento de ruído, bateria de 20 horas. Ótimo para estudar na biblioteca.",
            Categoria.Eletronicos, "fone", bruna, 5);
        var java = NovoObjeto("Livro Use a Cabeça! Java",
            "Jeito leve e visual de aprender orientação a objetos. 2ª edição, bem conservado.",
            Categoria.Livros, "livro-java", bruna, 3);
        var oculos = NovoObjeto("Óculos de proteção",
            "Óculos de segurança transparente, antiembaçante. Obrigatório nas aulas de laboratório de química.",
            Categoria.Laboratorio, "oculos", lucas, 1);
        db.Objetos.AddRange(calculadora, algoritmos, jaleco, guardaChuva, carregador, arduino, raquetes, fone, java, oculos);
        db.SaveChanges();

        var hoje = DateOnly.FromDateTime(DateTime.Now);
        db.Emprestimos.AddRange(
            // Emprestado agora: o livro da Ana está com a Carol
            new Emprestimo
            {
                ObjetoId = algoritmos.Id, SolicitanteId = carol.Id, Status = StatusEmprestimo.Emprestado,
                Mensagem = "Preciso para estudar para a prova de Estrutura de Dados!",
                DataSolicitacao = DateTime.UtcNow.AddDays(-4), DataResposta = DateTime.UtcNow.AddDays(-3),
                DataPrevistaDevolucao = hoje.AddDays(5)
            },
            // Aguardando a Carol aprovar
            new Emprestimo
            {
                ObjetoId = calculadora.Id, SolicitanteId = lucas.Id,
                Mensagem = "Oi Carol! Posso pegar para a prova de Cálculo II na quinta?",
                DataSolicitacao = DateTime.UtcNow.AddHours(-5), DataPrevistaDevolucao = hoje.AddDays(3)
            },
            new Emprestimo
            {
                ObjetoId = arduino.Id, SolicitanteId = ana.Id,
                Mensagem = "Quero testar um projeto de sensor para a disciplina de Sistemas Embarcados.",
                DataSolicitacao = DateTime.UtcNow.AddHours(-2), DataPrevistaDevolucao = hoje.AddDays(7)
            },
            // Histórico
            new Emprestimo
            {
                ObjetoId = jaleco.Id, SolicitanteId = carol.Id, Status = StatusEmprestimo.Devolvido,
                Mensagem = "Esqueci o meu em casa, devolvo no fim da aula!",
                DataSolicitacao = DateTime.UtcNow.AddDays(-10), DataResposta = DateTime.UtcNow.AddDays(-10),
                DataPrevistaDevolucao = hoje.AddDays(-9), DataDevolucao = DateTime.UtcNow.AddDays(-9)
            },
            new Emprestimo
            {
                ObjetoId = fone.Id, SolicitanteId = lucas.Id, Status = StatusEmprestimo.Devolvido,
                DataSolicitacao = DateTime.UtcNow.AddDays(-4), DataResposta = DateTime.UtcNow.AddDays(-4),
                DataPrevistaDevolucao = hoje.AddDays(-2), DataDevolucao = DateTime.UtcNow.AddDays(-2)
            }
        );
        algoritmos.Disponivel = false;
        db.SaveChanges();
    }
}
