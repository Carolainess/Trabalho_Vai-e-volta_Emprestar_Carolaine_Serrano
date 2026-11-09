# VAI E VOLTA

> **Compartilhe o que você tem. Encontre o que precisa.**

O VAI E VOLTA é uma plataforma web para estudantes da faculdade emprestarem objetos uns aos outros. Quem tem um livro, uma calculadora, um jaleco ou um carregador parado em casa anuncia no site. Quem precisa pede emprestado por alguns dias e devolve depois.

Fizemos o projeto em dupla com **ASP.NET Core MVC, Entity Framework Core, PostgreSQL e Docker**. Ele também está pronto para ser publicado na nuvem.

---

## Sumário

1. [Por que fizemos](#1-por-que-fizemos)
2. [O que dá para fazer](#2-o-que-dá-para-fazer)
3. [Tecnologias](#3-tecnologias)
4. [Como rodar](#4-como-rodar)
5. [Contas de demonstração](#5-contas-de-demonstração)
6. [Arquitetura](#6-arquitetura)
7. [Banco de dados](#7-banco-de-dados)
8. [API REST](#8-api-rest)
9. [Os três conceitos do trabalho](#9-os-três-conceitos-do-trabalho)
10. [Segurança](#10-segurança)
11. [Autoras](#11-autoras)

---

## 1. Por que fizemos

Todo semestre acontece a mesma coisa. Alguém precisa de uma calculadora científica só para a prova, esquece o jaleco no dia do laboratório ou precisa por duas semanas de um livro caro. E quase sempre um colega tem esse objeto parado em casa.

Hoje isso se resolve no grupo do WhatsApp: a mensagem se perde, ninguém lembra com quem ficou o livro e ninguém controla a devolução. No VAI E VOLTA cada objeto tem dono, situação e histórico de empréstimos.

## 2. O que dá para fazer

- **Criar conta, entrar e sair.** Os formulários são validados e a senha é guardada como hash. O login dura 7 dias.
- **Anunciar objetos** com foto (JPG, PNG ou WEBP até 5 MB), editar, pausar o anúncio ou excluir.
- **Procurar no catálogo:** pesquisar pelo nome, filtrar por categoria e mostrar só os disponíveis.
- **Ver os detalhes** de um objeto: foto, descrição, dono, quantas vezes já foi emprestado e o formulário para pedir.
- **Pedir emprestado**, com data de devolução e uma mensagem para o dono.
- **Responder aos pedidos** na tela de Empréstimos, nas abas *Recebidos* e *Meus pedidos*: aprovar, recusar, confirmar a devolução ou cancelar um pedido que ainda não foi respondido.
- **Ver o perfil**, com os dados da pessoa e um resumo da atividade.
- **Usar o painel do administrador:** números da plataforma e todos os empréstimos. O administrador pode aprovar pedidos, editar ou excluir qualquer objeto e tornar um usuário administrador (ou desfazer isso).
- **Encontrar páginas de erro claras** para 404, 403 e 500.

## 3. Tecnologias

| Parte | O que usamos |
|---|---|
| Linguagem | C# (.NET 8) |
| Site | ASP.NET Core MVC com Views Razor |
| API | Controllers REST (`[ApiController]`) na mesma aplicação |
| Banco | PostgreSQL 16 |
| Acesso ao banco | Entity Framework Core 8 + Npgsql, com migrations |
| Login | Cookie de autenticação + `PasswordHasher` |
| Interface | HTML, CSS, JavaScript, Bootstrap 5.3, Bootstrap Icons e fonte Plus Jakarta Sans |
| Contêineres | Docker e Docker Compose |
| Nuvem | Render.com (`render.yaml`) |

As cores são o azul-marinho (`#1b3561`), o laranja (`#f37021`) e o branco do Portal Acadêmico da FEMA. Elas ficam em variáveis no topo do `wwwroot/css/site.css`, e é só mudar ali para trocar o site inteiro.

As bibliotecas da interface estão dentro do projeto (`wwwroot/lib`), então o site funciona sem internet.

## 4. Como rodar

### Com Docker

Você precisa do Docker Desktop instalado e aberto.

```bash
# na pasta onde está o docker-compose.yml
docker compose up --build
```

A primeira vez demora alguns minutos porque o Docker baixa as imagens. Quando aparecer `Banco de dados pronto.` no terminal, abra **http://localhost:8080**.

Comandos que usamos bastante:

```bash
docker ps                      # mostra os 2 contêineres rodando
docker compose logs -f app     # mostra o log da aplicação
docker compose down            # para tudo
docker compose down -v         # para e apaga o banco (os dados de exemplo voltam na próxima vez)
```

### Sem Docker (Visual Studio ou VS Code)

1. Instale o .NET 8 SDK e deixe um PostgreSQL rodando na porta 5432, com usuário e senha `postgres`. Para subir só o banco, use `docker compose up banco`.
2. Na pasta `VaiEVolta`, rode `dotnet run`.
3. Abra o endereço que aparecer no terminal.

### Migrations

A migration inicial (`CriacaoInicial`) já está na pasta `Migrations`, e a aplicação aplica ela sozinha quando inicia. Se mudarmos algum Model, criamos uma nova:

```bash
dotnet tool install --global dotnet-ef     # só na primeira vez
dotnet ef migrations add NomeDaAlteracao
```

## 5. Contas de demonstração

Se o banco estiver vazio quando a aplicação iniciar, ela cria 5 usuários, 10 objetos com ilustrações e alguns empréstimos: um emprestado, pedidos aguardando aprovação e dois já devolvidos. Esses dados estão em `Data/SeedData.cs`.

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador | admin@vaievolta.com | Admin@123 |
| Estudante | carol@vaievolta.com | Senha@123 |
| Estudante | ana@vaievolta.com | Senha@123 |
| Estudante | lucas@vaievolta.com | Senha@123 |
| Estudante | bruna@vaievolta.com | Senha@123 |

A tela de login tem botões que preenchem essas contas com um clique.

## 6. Arquitetura

É **uma aplicação só**, sem microsserviços. Ela entrega as páginas do site e a API REST e conversa com o PostgreSQL pelo Entity Framework.

```
          Navegador (site)            Postman / outro app
                 │                           │
                 │ páginas HTML              │ JSON
                 ▼                           ▼
 ┌───────────────────────────────────────────────────────┐
 │             Aplicação ASP.NET Core MVC                │
 │                                                       │
 │  Controllers (MVC)          Controllers/Api (REST)    │
 │  Home, Conta, Objetos,      /api/objetos              │
 │  Emprestimos, Admin         /api/emprestimos          │
 │                             /api/auth                 │
 │          │                         │                  │
 │          └────────┬────────────────┘                  │
 │                   ▼                                   │
 │      Services: EmprestimoService (regras)             │
 │                FotoService (upload de fotos)          │
 │                   │                                   │
 │      Data: AppDbContext (Entity Framework Core)       │
 └───────────────────┬───────────────────────────────────┘
                     ▼
             ┌───────────────┐
             │  PostgreSQL   │
             └───────────────┘
```

As regras de empréstimo ficam todas no `EmprestimoService`, e o site e a API usam o mesmo serviço. Por isso aprovar um pedido pela tela ou pela API faz exatamente a mesma coisa.

### Pastas

```
vaievolta/
├── Dockerfile                 → imagem da aplicação
├── docker-compose.yml         → aplicação + PostgreSQL
├── render.yaml                → publicação no Render
├── README.md
├── ROTEIRO-APRESENTACAO.md    → roteiro da apresentação
└── VaiEVolta/
    ├── Program.cs             → configuração (banco, login, porta, health check)
    ├── Controllers/
    │   ├── HomeController.cs        → página inicial, página da API, erros
    │   ├── ContaController.cs       → cadastro, login, logout, perfil
    │   ├── ObjetosController.cs     → catálogo, detalhes, cadastrar, editar, excluir
    │   ├── EmprestimosController.cs → pedir, aprovar, recusar, devolver, cancelar
    │   ├── AdminController.cs       → painel do administrador
    │   └── Api/                     → endpoints REST
    ├── Models/                → Usuario, Objeto, Emprestimo, ViewModels e DTOs
    ├── Data/                  → AppDbContext, conexão com o banco e dados de exemplo
    ├── Migrations/            → criação das tabelas
    ├── Services/              → regras de empréstimo e upload de fotos
    ├── ViewComponents/        → contador de pedidos pendentes no menu
    ├── Views/                 → telas Razor
    └── wwwroot/               → CSS, JS, imagens e bibliotecas
```

## 7. Banco de dados

São três tabelas:

```
┌──────────────────┐        ┌──────────────────────┐        ┌──────────────────────────┐
│     Usuario      │        │        Objeto        │        │        Emprestimo        │
├──────────────────┤        ├──────────────────────┤        ├──────────────────────────┤
│ Id (PK)          │◄──┐    │ Id (PK)              │◄───────│ ObjetoId (FK)            │
│ Nome             │   └────│ DonoId (FK)          │        │ Id (PK)                  │
│ Email (único)    │        │ Nome                 │   ┌────│ SolicitanteId (FK)       │
│ SenhaHash        │◄───────┼──────────────────────┼───┘    │ Mensagem                 │
│ Curso            │        │ Descricao            │        │ DataSolicitacao          │
│ Telefone         │        │ Categoria            │        │ DataPrevistaDevolucao    │
│ Perfil           │        │ FotoUrl              │        │ DataResposta             │
│ CriadoEm         │        │ Disponivel           │        │ DataDevolucao            │
└──────────────────┘        │ CriadoEm             │        │ Status                   │
                            └──────────────────────┘        └──────────────────────────┘
```

- Um **usuário** pode ter vários **objetos** (ele é o dono).
- Um **objeto** pode ter vários **empréstimos** ao longo do tempo, e isso forma o histórico.
- Um **usuário** pode fazer vários **pedidos de empréstimo** (ele é o solicitante).

**Valores fixos (enums)**

- `Perfil`: Usuario ou Administrador.
- `Categoria`: Livros, Eletrônicos, Material de estudo, Laboratório, Esporte e lazer, Outros.
- `StatusEmprestimo`: Aguardando aprovação, Emprestado, Devolvido, Recusado.

### Como a situação do objeto muda

O card de cada objeto mostra uma destas situações: **Disponível**, **Aguardando aprovação**, **Emprestado** ou **Indisponível** (quando o dono pausou o anúncio). Ninguém escolhe a situação. Ela muda com os empréstimos:

| Ação | O que muda no banco |
|---|---|
| Alguém pede | cria um empréstimo com status *Aguardando aprovação* |
| O dono aprova | o status vira *Emprestado*, o objeto fica `Disponivel = false` e os outros pedidos pelo mesmo objeto são recusados sozinhos |
| O dono recusa | o status vira *Recusado* |
| Quem pediu cancela | o pedido é apagado (só enquanto ainda não foi respondido) |
| O dono confirma a devolução | o status vira *Devolvido* e o objeto volta a `Disponivel = true` |

Algumas regras que o sistema confere antes de aceitar um pedido: ninguém pode pedir o próprio objeto, a data de devolução não pode estar no passado e a mesma pessoa não pode ter dois pedidos aguardando para o mesmo objeto.

## 8. API REST

Tudo o que o site faz também funciona pela API, em JSON. A base é `http://localhost:8080`.

Os endpoints marcados com 🔒 exigem login. A API usa o mesmo cookie do site. Para testar no Postman, chame primeiro `POST /api/auth/login`. Sem login a API responde **401**, e sem permissão responde **403**, sempre em JSON e nunca redirecionando para a tela de login.

Também dá para testar pelo próprio site em **http://localhost:8080/Home/Api**. Essa página não aparece no menu, só pelo endereço.

### Objetos

| Método | Endpoint | O que faz | Respostas |
|---|---|---|---|
| GET | `/api/objetos` | Lista os objetos. Filtros: `?busca=`, `?categoria=`, `?disponiveis=true` | 200 |
| GET | `/api/objetos/{id}` | Mostra um objeto | 200, 404 |
| POST 🔒 | `/api/objetos` | Cadastra um objeto | 201, 400, 401 |
| PUT 🔒 | `/api/objetos/{id}` | Atualiza (só o dono ou o admin) | 200, 400, 403, 404 |
| DELETE 🔒 | `/api/objetos/{id}` | Exclui (só o dono ou o admin; não exclui se estiver emprestado) | 204, 403, 404, 409 |

Exemplo de corpo para POST e PUT:

```json
{
  "nome": "Régua T de desenho técnico",
  "descricao": "Régua T de 80 cm, ótima para Desenho Técnico.",
  "categoria": "MaterialDeEstudo",
  "disponivel": true
}
```

### Empréstimos

| Método | Endpoint | O que faz | Respostas |
|---|---|---|---|
| GET 🔒 | `/api/emprestimos` | Empréstimos em que você participa (o admin vê todos). Filtro: `?status=Emprestado` | 200 |
| GET 🔒 | `/api/emprestimos/{id}` | Mostra um empréstimo | 200, 403, 404 |
| POST 🔒 | `/api/emprestimos` | Pede um objeto emprestado | 201, 400 |
| PUT 🔒 | `/api/emprestimos/{id}/aprovar` | Aprova (dono ou admin) | 200, 400 |
| PUT 🔒 | `/api/emprestimos/{id}/recusar` | Recusa (dono ou admin) | 200, 400 |
| PUT 🔒 | `/api/emprestimos/{id}/devolver` | Confirma a devolução (dono ou admin) | 200, 400 |
| DELETE 🔒 | `/api/emprestimos/{id}` | Cancela um pedido que ainda está aguardando (só quem pediu) | 204, 400 |

```json
{
  "objetoId": 4,
  "dataPrevistaDevolucao": "2026-11-20",
  "mensagem": "Posso pegar para a aula de quinta?"
}
```

### Login

| Método | Endpoint | O que faz |
|---|---|---|
| POST | `/api/auth/login` | Recebe `{ "email": "...", "senha": "..." }` e devolve o cookie de sessão |
| POST | `/api/auth/logout` | Encerra a sessão |
| GET 🔒 | `/api/auth/eu` | Mostra quem está logado |

### Health check

`GET /health` responde `Healthy` quando a aplicação está no ar e consegue falar com o banco. A nuvem usa esse endereço.

## 9. Os três conceitos do trabalho

### 1. API REST

**Arquivos:** `Controllers/Api/ObjetosApiController.cs`, `EmprestimosApiController.cs` e `AuthApiController.cs`.

- Cada recurso tem a sua URL (`/api/objetos`, `/api/emprestimos`).
- O verbo HTTP diz a ação: **GET** consulta, **POST** cria, **PUT** atualiza e **DELETE** remove.
- Os dados vão e voltam em **JSON**.
- O **código de status** diz o que aconteceu: 200 OK, 201 Created, 204 No Content, 400 dados inválidos, 401 sem login, 403 sem permissão, 404 não encontrado e 409 conflito (por exemplo, tentar excluir um objeto que está emprestado).
- A API não guarda estado entre uma chamada e outra. Cada requisição leva o cookie que diz quem está chamando.

### 2. Docker

**Arquivos:** `Dockerfile`, `docker-compose.yml` e `.dockerignore`.

- O `Dockerfile` tem **duas etapas**. A primeira usa a imagem do SDK para compilar. A segunda usa só o runtime do ASP.NET, e por isso a imagem final é menor.
- O `docker-compose.yml` sobe **dois contêineres**: `app` (o site) e `banco` (PostgreSQL 16).
- O `healthcheck` do banco faz a aplicação esperar o PostgreSQL ficar pronto antes de subir. Por garantia, a aplicação ainda tenta se conectar até 10 vezes.
- Os **volumes** guardam os dados do banco, as fotos enviadas e as chaves do login. Nada se perde quando o contêiner é recriado.
- A conexão com o banco chega por **variável de ambiente** (`ConnectionStrings__Padrao`), sem mudar o código.

### 3. Computação em nuvem

**Arquivos:** `render.yaml`, `Program.cs` e `Data/ConexaoBanco.cs`.

A aplicação está preparada para rodar em uma plataforma de nuvem (PaaS) como Render, Railway ou Azure App Service:

- **Configuração por variáveis de ambiente.** A porta vem de `PORT` e o banco de `DATABASE_URL`, que é o formato que esses serviços entregam.
- **Health check em `/health`.** A nuvem acessa esse endereço para saber se o site está funcionando e reinicia o site se precisar.
- **HTTPS na frente da aplicação.** A nuvem cuida do HTTPS, e o `UseForwardedHeaders` faz o ASP.NET entender isso.
- **Banco gerenciado.** Usamos o PostgreSQL que o provedor oferece, sem instalar nada.
- **Infraestrutura como código.** O `render.yaml` descreve o site e o banco, e o Render cria os dois a partir dele.
- **Migrations na inicialização.** Ao publicar, as tabelas e os dados de exemplo são criados sem nenhum passo manual.

#### Publicar no Render (plano gratuito)

1. Suba esta pasta para um repositório no GitHub.
2. Crie uma conta em [render.com](https://render.com) entrando com o GitHub.
3. Clique em **New → Blueprint** e escolha o repositório.
4. O Render lê o `render.yaml`, cria o PostgreSQL e o site e liga um no outro.
5. Espere o deploy terminar (alguns minutos). O endereço fica parecido com `https://vaievolta.onrender.com`.

> No plano gratuito o site "dorme" depois de um tempo sem acesso e leva uns 30 segundos para acordar, então vale abrir alguns minutos antes da apresentação. As fotos enviadas ficam no disco do contêiner e podem sumir quando ele é recriado. Os dados do banco continuam salvos.

## 10. Segurança

- Senhas guardadas com **hash**, nunca em texto puro.
- Todos os formulários têm **token antifalsificação** contra CSRF.
- Rotas protegidas com `[Authorize]` e `[Authorize(Roles = "Administrador")]`.
- Antes de alterar um objeto, o sistema confere se a pessoa é a dona (ou o admin).
- O upload só aceita imagens de até 5 MB, e cada arquivo é salvo com um nome aleatório.
- Depois do login, o site só redireciona para endereços dele mesmo.

## 11. Autoras

- Carolaine Silva Serrano — Ciência da Computação, FEMA
- *[nome da dupla]* — Ciência da Computação, FEMA

---

Projeto acadêmico · FEMA · Assis/SP · 2026
