# Plano de execução — ExpenseHub

Plano de trabalho do grupo para o Checkpoint 2. Prazo: **13 de outubro de 2026**.

Integrantes:

- **Gustavo Laur Pisanello | RM:556603**

- **Leonardo de Farias RM:555211**

Este documento resume os requisitos de `docs/` e das issues do backlog central
(`Racass/checkpoint-csharpracass-expensehub`) e define **quem faz o quê e em que ordem**.
Ele pode ser entregue a um assistente de IA como contexto — veja a seção
[Instruções para assistentes de IA](#10-instruções-para-assistentes-de-ia).

Em caso de divergência, valem os documentos oficiais:
[ENUNCIADO](ENUNCIADO.md), [REQUISITOS](REQUISITOS.md),
[MATRIZ-AUTORIZACAO](MATRIZ-AUTORIZACAO.md), [RUBRICA](RUBRICA.md),
[PROCESSO-GITHUB](PROCESSO-GITHUB.md), [USO-DE-IA](USO-DE-IA.md) e
[code-quality-rules](code-quality-rules.md).

---

## 1. Visão geral da nota

| Issue | Conteúdo | Peso | Depende de | Responsável |
|---|---|---:|---|---|
| I01 | Fundação da solução e EF Core | 4% | — | Gustavo |
| I02 | Identity, Admin e autenticação | 9% | I01 | Leonardo |
| I03 | Cadastro HTTP e gerenciamento de roles | 8% | I02 | Gustavo |
| I04 | Criar e editar rascunho | 7% | I01, I03 | Leonardo |
| I05 | Enviar, listar e consultar | 7% | I04 | Gustavo |
| I06 | Ownership e matriz de acesso | 10% | I02, I05 | Leonardo |
| I07 | Aprovar e reprovar com justificativa | 12% | I06 | Gustavo |
| I08 | Pagamento e histórico | 8% | I06, I07 | Leonardo |
| I09 | Testes unitários | 10% | transversal | Ambos |
| I10 | Qualidade de Código (score do CI) | **25%** | transversal | Ambos |

Gates que limitam a nota final:

| Condição | Nota máxima |
|---|---:|
| Projeto não compila no ambiente oficial | 2,0 |
| Autorização sistematicamente ausente | 4,0 |
| Sem testes unitários próprios significativos | 8,0 |

## 2. Regras do processo (valem para tudo)

- **Todo integrante precisa de mais de um commit.** Commits vazios, artificiais,
  divididos sem motivo ou feitos por outra pessoa **não contam**. Cada um commita
  da própria máquina, com a própria conta.
- **Uma branch e uma pull request por issue**, no nosso repositório.
- Título da PR: `I04 — Criar e editar rascunho`.
- Na descrição da PR, cite `Racass/checkpoint-csharpracass-expensehub#4`.
  **Nunca** use `Closes`, `Fixes` ou `Resolves`.
- Aguardar o workflow `code-quality`, ler o relatório, corrigir e só então fazer merge.
- **Não alterar** `.editorconfig`, `Directory.Build.props`, `scripts/`, `tests/*.ps1`
  nem `.github/workflows/`. Mexer nisso para melhorar a pontuação pode **zerar a I10**.
- Nunca versionar senhas, tokens, connection strings sensíveis, `bin/`, `obj/`, `.vs/`
  ou arquivos de banco.
- Mensagens de commit objetivas, no estilo:

  ```text
  feat(expenses): enforce draft ownership on updates
  test(expenses): cover invalid approval transitions
  docs: document SQLite setup
  ```

### Descrição padrão da PR

```markdown
Issue: Racass/checkpoint-csharpracass-expensehub#N

## Resumo técnico
## Decisões e concessões
## Como validar
## Evidências
## Impactos em segurança e autorização

- [ ] Critérios de aceite atendidos
- [ ] Casos negativos validados
- [ ] Autorização revisada
- [ ] Testes unitários adicionados ou atualizados
- [ ] Build sem erros
- [ ] Pipeline analisado
- [ ] Documentação atualizada
```

## 3. Armadilhas do pipeline de qualidade (I10 = 25%)

A I10 usa diretamente o score 0–100 do workflow. Cada categoria (Build, Segurança,
Higiene, Estilo, Boas práticas) vale 20 pontos, e cada regra desconta uma vez.

1. **Qualquer warning de compilador (`CSxxxx`) desconta 20 pontos**, zerando a
   categoria Build. Os mais prováveis:
   - **CS1591** — `GenerateDocumentationFile` está ligado, então todo tipo e membro
     `public` precisa de `/// <summary>`. Isso inclui controllers, DTOs, entidades,
     serviços **e as classes e métodos de teste** (MSTest exige que sejam públicos).
     Onde der, use `internal` e `InternalsVisibleTo("ExpenseHub.UnitTests")`.
   - **CS8618 / CS86xx** — `Nullable` está habilitado. Inicialize strings com
     `string.Empty` (e não `""`, por causa da regra SA1122) ou use `required`.
2. **`ImplicitUsings` está desligado**: todo `using` é explícito, e using sobrando
   gera IDE0005.
3. `IDE*` desconta 1 ponto por regra e `CA*` desconta 2 (4 se for de segurança).
   Os mais comuns:
   - chaves obrigatórias em todo `if`;
   - `readonly` em campos;
   - modificador de acesso explícito;
   - não usar `this.`;
   - `using` fora do namespace;
   - CA1305 (informar `CultureInfo`);
   - CA2016 (repassar o `CancellationToken`);
   - CA1849 (usar a versão assíncrona).
4. Formatação (IDE0055): rodem `dotnet format ./sources/ExpenseHub.slnx` antes
   de commitar.
5. **FIAP3005**: todo arquivo numa pasta `Dto/` ou `Dtos/` (sem diferenciar
   maiúsculas) precisa ter pelo menos um atributo `[Required]`, `[Range]`,
   `[StringLength]`, `[MinLength]`, `[MaxLength]`, `[EmailAddress]` ou
   `[RegularExpression]`. DTOs **de resposta** vão em outra pasta (`Contracts/`).
6. **FIAP3004**: arquivos em `Controllers/` não podem receber como parâmetro uma
   classe declarada em `Models/`. Sempre use DTO.
7. **FIAP1002 (bloqueante)**: em `.json` ou `.cs` versionado, nenhuma chave com
   `password`, `pwd`, `senha`, `secret`, `token`, `apikey` ou `connectionstring` pode
   ter valor preenchido.
   - A senha do Admin vem de `dotnet user-secrets` ou de variável de ambiente.
   - Connection string do SQLite no formato aninhado:

     ```json
     "ConnectionStrings": {
       "Default": "Data Source=expensehub.db"
     }
     ```

8. **FIAP3001 / FIAP3003**: use `AnyAsync` (nunca `CountAsync(...) > 0`) e nunca
   `_context.X.Any(...)` síncrono.
9. **FIAP4001**: se algum teste falhar, desconta 4 pontos.
10. Gitleaks roda no histórico inteiro. **Um segredo commitado continua no
    histórico mesmo que seja apagado depois.**

## 4. Decisões técnicas

| Tema | Decisão |
|---|---|
| Framework | .NET 10 (`net10.0`), ASP.NET Core |
| Banco | **SQLite** (`Microsoft.EntityFrameworkCore.Sqlite`), sem servidor |
| Migrations | `Microsoft.EntityFrameworkCore.Design` + `dotnet-ef` |
| Identity | `AddIdentityCore<IdentityUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddSignInManager()` |
| Autenticação | Bearer token do Identity: `AddAuthentication(IdentityConstants.BearerScheme).AddBearerToken(IdentityConstants.BearerScheme)`. Não precisa de chave JWT para esconder |
| `/register` e `/login` | Escritos por nós, com `UserManager` e `SignInManager`. **Não** usar `MapIdentityApi`, que expõe rotas extras fora do contrato |
| API | Controllers com `[ApiController]` (validação automática com resposta 400) |
| Erros | `AddProblemDetails()` + tratamento de exceções de domínio convertendo para 400, 403, 404 e 409 |
| Relógio | `TimeProvider` injetado, para os testes controlarem a hora |
| Testes | MSTest, **sem banco**. Testar domínio, policy de acesso e serviços com fakes |

### Estrutura de pastas sugerida

```text
sources/ExpenseHub.Api/
├── Models/        Expense, ExpenseCategory, ExpenseHistory, PaymentRecord, ExpenseStatus
├── Data/          AppDbContext, configurações Fluent, DbSeeder
├── Migrations/    gerado pelo EF
├── Dtos/          requests (com Data Annotations)
├── Contracts/     responses
├── Services/      ExpenseService, ExpenseAccessPolicy, UserAdminService, exceções de domínio
├── Controllers/   AuthController, AdminUsersController, ExpensesController
└── Program.cs
```

### Modelo de dados mínimo

- `ExpenseStatus`: `Draft`, `Submitted`, `Approved`, `Rejected` e `Paid`.
- `Expense`:
  - `Id` (gerado pelo servidor), `OwnerId`, `CategoryId`;
  - `Description` (10 a 500), `Amount` (`decimal`, com precisão configurada), `ExpenseDate` (`DateOnly`);
  - `Status`, `CreatedAtUtc`, `UpdatedAtUtc`;
  - `Version` (token de concorrência).
- `ExpenseCategory`: `Id`, `Name`. As categorias podem ser semeadas com `HasData`;
  a restrição de seed vale **só para usuários**.
- `ExpenseHistory`:
  - `Id`, `ExpenseId`, `Action`, `ActorId`, `OccurredAtUtc`;
  - `FromStatus` (anulável), `ToStatus`;
  - `Justification` (anulável), `Changes` (anulável; descreve o que mudou em Draft).
- `PaymentRecord`: `Id`, `ExpenseId` (único), `PaidById`, `PaidAtUtc`, `Amount`.
- **Um reembolso tem um único valor.** Nada de coleção de itens.

### Regras de transição (no domínio)

| Estado atual | Ação | Quem | Próximo |
|---|---|---|---|
| — | criar | Employee | `Draft` |
| `Draft` | editar | proprietário | `Draft` |
| `Draft` | enviar | proprietário | `Submitted` |
| `Submitted` | aprovar | Approver, exceto proprietário | `Approved` |
| `Submitted` | reprovar (com justificativa) | Approver, exceto proprietário | `Rejected` |
| `Approved` | pagar | Finance, exceto proprietário | `Paid` |

`Rejected` e `Paid` são finais. Não existe reabertura, cancelamento, exclusão ou
reenvio. Os métodos `Submit`, `Approve`, `Reject` e `Pay` da entidade validam o
estado, lançam conflito quando o estado não permite e geram o `ExpenseHistory`.
A alteração e o histórico são gravados **no mesmo `SaveChangesAsync`**.

### Visibilidade (filtro de leitura)

A policy gera **uma** expressão aplicada ao `IQueryable` **antes** do
`ToListAsync`, nunca em memória:

```csharp
e => (isEmployee && e.OwnerId == userId)
  || (isApprover && e.Status == ExpenseStatus.Submitted)
  || (isFinance && (e.Status == ExpenseStatus.Approved || e.Status == ExpenseStatus.Paid))
  || isAuditor
```

Com várias roles, o usuário recebe a união. Admin sem outra role não acessa as
rotas de reembolso e recebe **403**.

### Ordem das checagens (contrato de status HTTP)

1. Sem token ou token inválido: **401** (feito pelo framework).
2. Nenhuma role permitida na rota (`[Authorize(Roles = "...")]`): **403**.
3. Corpo inválido: **400** (feito pelo `[ApiController]`).
4. Despesa inexistente ou fora do escopo da ação: **404**. O escopo depende da ação:
   - leitura e histórico: o filtro de visibilidade acima;
   - editar e enviar: somente despesas próprias;
   - aprovar, reprovar e pagar: despesas que não estão em `Draft`. Assim uma
     repetição (ex.: aprovar algo já `Approved`) responde 409, e rascunhos alheios
     continuam ocultos.
5. Proprietário tentando aprovar, reprovar ou pagar a própria despesa: **403**.
6. Estado incompatível ou transição repetida: **409**, sem gravar histórico.

Registrem essa decisão na seção "Decisões e concessões" das PRs I05, I06 e I07.

### Contrato dos endpoints

| Método e rota | Roles | Corpo | Sucesso |
|---|---|---|---|
| `POST /register` | público | `{ email, password }` (sem campo de role) | 201 |
| `POST /login` | público | `{ email, password }` | 200 com token |
| `GET /api/admin/users` | Admin | — | 200 com lista de `{ id, email, roles }` |
| `PUT /api/admin/users/{id}/roles` | Admin | `{ roles: [...] }` (substitui o conjunto) | 200 |
| `POST /api/expenses` | Employee | `{ description, amount, expenseDate, categoryId }` | 201 |
| `PUT /api/expenses/{id}` | Employee | mesmo corpo | 200 |
| `GET /api/expenses` | Employee, Approver, Finance, Auditor | — | 200 |
| `GET /api/expenses/{id}` | Employee, Approver, Finance, Auditor | — | 200 |
| `POST /api/expenses/{id}/submit` | Employee | — | 200 |
| `POST /api/expenses/{id}/approve` | Approver | — | 200 |
| `POST /api/expenses/{id}/reject` | Approver | `{ justification }` (10 a 500) | 200 |
| `POST /api/expenses/{id}/pay` | Finance | — | 200 |
| `GET /api/expenses/{id}/history` | Employee, Approver, Finance, Auditor | — | 200 |

Os DTOs **nunca** têm `OwnerId`, `Status`, ator ou datas de sistema.

## 5. Ordem de execução

```text
I01 (Gustavo)
  └─ I02 (Leonardo)
       ├─ I03 (Gustavo)  ┐ em paralelo
       └─ I04 (Leonardo) ┘
            └─ I05 (Gustavo)
                 └─ I06 (Leonardo)
                      └─ I07 (Gustavo)
                           └─ I08 (Leonardo)
                                └─ I09 + I10 (ambos)
```

Só comece uma etapa depois do merge da anterior na `main`. A exceção é o par
I03 ∥ I04.

## 6. Passo a passo por etapa

Cada item numerado é, em geral, um commit.

### Etapa 1 — I01 Fundação e EF Core · **Gustavo** · branch `i01-foundation-ef`

1. `chore: ignore SQLite database files`: adicionar `*.db`, `*.db-shm` e
   `*.db-wal` ao `.gitignore`.
2. `feat(data): add domain entities and status enum`: as 4 entidades e o enum.
3. `feat(data): configure AppDbContext with SQLite`: pacotes EF Core 10.x (mesma
   versão em todos), `AppDbContext : IdentityDbContext`, mapeamentos Fluent
   (precisão do decimal, tamanhos máximos, relacionamentos, índice único de
   `PaymentRecord.ExpenseId`), connection string e registro no `Program.cs`.
4. `feat(data): add initial migration`: `dotnet ef migrations add InitialCreate`.
   A pasta `Migrations/` é tratada como código gerado pelo `.editorconfig`.
5. `docs: document database setup`: no `README.md`, documentar provider, pacote,
   configuração, `dotnet ef database update` e como iniciar a aplicação.

Critérios de aceite:

- a solução compila;
- o provider foi escolhido e o contexto e os mapeamentos estão implementados;
- o banco é criado de forma reproduzível;
- o processo está documentado sem segredos.

Casos negativos:

- o build não depende de banco rodando;
- os testes não dependem do provider;
- banco local e credenciais não são versionados.

### Etapa 2 — I02 Identity e autenticação · **Leonardo** · branch `i02-identity-auth`

1. `feat(auth): configure Identity and bearer authentication`: Identity
   persistido no `AppDbContext`, esquema bearer, `UseAuthentication` e
   `UseAuthorization`, e uma migration nova para as tabelas do Identity.
2. `feat(auth): add login endpoint`: `POST /login` devolve o token para
   credenciais válidas e 401 para inválidas.
3. `feat(auth): seed roles and initial admin idempotently`:
   - cria as roles `Admin`, `Employee`, `Approver`, `Finance` e `Auditor` só se não existirem;
   - cria **uma** conta Admin só se ela não existir;
   - e-mail e senha vêm da configuração (`Seed:AdminEmail` e `Seed:AdminPassword`, via
     `dotnet user-secrets` ou variáveis `Seed__AdminEmail` e `Seed__AdminPassword`);
   - sem senha configurada, a aplicação falha com mensagem clara, sem logar o valor;
   - aplicar migrations e rodar o seed na inicialização.
4. `docs: document admin secret configuration`: comandos `dotnet user-secrets init`
   e `set` no README.
5. Adicionar ao `ExpenseHub.Api.http` requisições de login válido e inválido, rota
   sem token (401) e usuário sem role (403).

Critérios de aceite:

- Identity com persistência relacional;
- login emite o bearer;
- as 5 roles existem;
- o seed é idempotente e só cria o Admin;
- a senha fica fora do código;
- 401 e 403 são distintos.

Casos negativos:

- credencial inválida não gera token;
- reexecutar o seed não duplica nada;
- usuário sem role não faz operação privilegiada;
- segredo não aparece em commit ou log.

### Etapa 3a — I03 Cadastro e roles · **Gustavo** · branch `i03-user-roles`

1. `feat(auth): add public registration endpoint`: `POST /register` com DTO só de
   `Email` (`[Required]`, `[EmailAddress]`) e `Password` (`[Required]`). Erros do
   Identity (senha fraca, e-mail duplicado) respondem 400 com ProblemDetails. O
   usuário é criado **sem nenhuma role**.
2. `feat(admin): list users for admins`: `GET /api/admin/users` com
   `[Authorize(Roles = "Admin")]`, devolvendo id, e-mail e roles.
3. `feat(admin): update user roles`: `PUT /api/admin/users/{id}/roles`.
   - Role desconhecida: 400. **Nunca** criar role implicitamente.
   - Usuário inexistente: 404.
   - Admin removendo a própria role Admin: 400.
   - Após a mudança, chamar `UpdateSecurityStampAsync`.
4. `test(admin): cover role management rules`: testes do serviço de roles, com
   `UserManager` e `RoleManager` falsos ou uma interface própria.
5. `docs: document role management and re-login`: README avisando que o usuário
   precisa fazer login de novo depois de uma mudança de roles.

Casos negativos:

- o cadastro não promove o usuário;
- Employee, Approver, Finance e Auditor não administram roles (403);
- role arbitrária não é criada;
- o Admin não remove a própria role Admin.

### Etapa 3b — I04 Criar e editar rascunho · **Leonardo** · branch `i04-expense-draft`

Pode começar junto com a I03. Para testar antes do merge da I03, atribua a role
`Employee` manualmente no banco local.

1. `feat(expenses): add create and update request DTOs`:
   - `Description`: `[Required, StringLength(500, MinimumLength = 10)]`.
   - `Amount`: `[Range(typeof(decimal), "0.01", "2147483647", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]`.
   - `ExpenseDate`: obrigatória e não futura (validação própria usando `TimeProvider`).
   - `CategoryId`: obrigatório; categoria inexistente responde 400.
2. `feat(expenses): create draft expenses`: `POST /api/expenses` com
   `[Authorize(Roles = "Employee")]`. O owner vem do token
   (`ClaimTypes.NameIdentifier`), o estado é `Draft` e é gravado o histórico
   `Created`.
3. `feat(expenses): enforce draft ownership on updates`: `PUT /api/expenses/{id}`.
   - Despesa de outro usuário ou inexistente: 404.
   - Despesa própria fora de Draft: 409.
   - Histórico `Updated` registrando os campos alterados.
4. `test(expenses): cover draft validation and ownership`.

Casos negativos:

- valor < 0,01 ou > `Int32.MaxValue`, data futura e descrição fora dos limites dão 400;
- outro usuário não edita;
- despesa fora de Draft não é editada;
- os DTOs impedem mass assignment.

### Etapa 4 — I05 Enviar, listar e consultar · **Gustavo** · branch `i05-submit-query`

1. `feat(expenses): submit draft expenses`: `Expense.Submit` (Draft → Submitted,
   com histórico) e `POST /api/expenses/{id}/submit`, só para o proprietário.
   Repetição responde 409.
2. `feat(expenses): add role-based visibility policy`: `ExpenseAccessPolicy` com
   a expressão da seção 4.
3. `feat(expenses): list and detail visible expenses`: `GET /api/expenses` e
   `GET /api/expenses/{id}`, com o filtro no `IQueryable`. Fora do escopo responde
   404. A rota exige Employee, Approver, Finance ou Auditor (Admin puro recebe 403).
4. `test(expenses): cover submission and visibility rules`: testar a policy
   compilando a expressão e aplicando numa lista em memória.

Casos negativos:

- outro Employee não envia o Draft;
- Submitted não é reenviado;
- Auditor não escreve;
- Admin não ganha acesso funcional.

### Etapa 5 — I06 Ownership e matriz de acesso · **Leonardo** · branch `i06-ownership-access`

1. Revisar rota por rota contra [MATRIZ-AUTORIZACAO](MATRIZ-AUTORIZACAO.md):
   - o atributo de role está correto no controller;
   - a checagem de ownership e estado está **no serviço**.
2. `feat(expenses): centralize contextual authorization in service`: garantir
   que acumular roles não libera nada indevido. Exemplos:
   - Approver sem Employee não cria despesa;
   - Employee + Approver não aprova a própria despesa.
3. `test(expenses): cover access matrix negative cases`:
   - Employee acessando despesa de outro (404);
   - troca de id na URL;
   - Admin puro (403);
   - Auditor tentando escrever (403);
   - filtros de Approver e Finance.
4. `docs: add multi-identity requests to http file`: requisições no `.http` com
   vários usuários (Employee A, Employee B, Approver, Finance, Auditor e Admin).

Critérios de aceite:

- isolamento entre Employees;
- Approver vê Submitted e Finance vê Approved/Paid;
- Auditor vê tudo sem escrever;
- Admin sem acesso implícito;
- decisões contextuais no serviço;
- 401, 403 e 404 corretos.

### Etapa 6 — I07 Aprovar e reprovar · **Gustavo** · branch `i07-approve-reject`

1. `feat(expenses): approve submitted expenses`: `Expense.Approve(actorId, now)`
   (Submitted → Approved, com histórico) e `POST /api/expenses/{id}/approve` com
   `[Authorize(Roles = "Approver")]`. O proprietário recebe 403.
2. `feat(expenses): reject with justification`: DTO `RejectExpenseRequest` com
   `Justification` `[Required, StringLength(500, MinimumLength = 10)]`, persistida
   no histórico, e `POST /api/expenses/{id}/reject`.
3. `feat(expenses): detect concurrent transitions`: usar o `Version` como token de
   concorrência. `DbUpdateConcurrencyException` responde 409.
4. `test(expenses): cover approval and rejection rules`.

Casos negativos:

- Employee, Finance, Auditor e Admin sem Approver não decidem;
- Approver proprietário não decide;
- justificativa vazia, curta ou longa é rejeitada;
- Approved, Rejected e Paid não recebem nova decisão (409 sem histórico duplicado).

### Etapa 7 — I08 Pagamento e histórico · **Leonardo** · branch `i08-payment-history`

1. `feat(expenses): pay approved expenses`: `Expense.Pay(actorId, now)`
   (Approved → Paid) cria o `PaymentRecord` com ator e horário do servidor.
   `POST /api/expenses/{id}/pay` com `[Authorize(Roles = "Finance")]`. O
   proprietário recebe 403.
2. `feat(expenses): expose expense history`: `GET /api/expenses/{id}/history`,
   com a mesma visibilidade da despesa e ordenado por data.
3. Conferir que toda operação grava estado, histórico e pagamento num **único**
   `SaveChangesAsync`.
4. `test(expenses): cover payment and history rules`.

Casos negativos:

- Submitted, Rejected e Paid não são pagos (409);
- pagamento repetido responde 409;
- Finance proprietário não paga;
- Auditor consulta, mas não altera;
- falha de persistência não deixa estado e histórico divergentes.

### Etapa 8 — I09 Testes unitários · **Ambos** · branch `i09-unit-tests`

Cada um já escreveu testes nas próprias features. Aqui fechem as lacunas:

- **Gustavo**: estados e transições válidas e inválidas, aprovação, reprovação,
  justificativa e validações de DTO.
- **Leonardo**: ownership, autoaprovação, autopagamento, filtros por perfil,
  pagamento e histórico.

Regras:

- sem banco, rede ou serviço externo;
- nomes que descrevem a regra, por exemplo
  `Approve_WhenActorIsOwner_ThrowsForbidden`;
- teste de mutação manual: quebrem uma regra de propósito (por exemplo, removam a
  checagem de proprietário) e confirmem que algum teste falha;
- testes de getters, de mocks ou só para aumentar cobertura não contam.

Pontuação: estados 2%, ownership 3%, decisões e pagamento 2%, histórico e
validações 1%, isolamento e valor contra regressão 2%.

### Etapa 9 — I10 Qualidade e entrega · **Ambos** · branch `i10-code-quality`

1. Baixar o artefato `code-quality-report` do último run (o `report.md` é o mais
   fácil de ler). Cada um zera os findings do código que escreveu.
2. `dotnet restore`, `dotnet build` e `dotnet test` sem erros e **sem warnings**.
3. README final:
   - provider e pacote;
   - segredos (user-secrets);
   - migrations;
   - como executar;
   - fluxo de exemplo com o `.http`;
   - aviso de novo login após mudança de roles.
4. Abrir a PR final com o **score registrado**. Depois do merge, anotar o SHA
   final da `main`.

## 7. Rotina de cada item

```shell
git switch main
git pull
git switch -c i0X-nome-da-branch
# ... implementar com commits pequenos ...
dotnet format ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet test ./sources/ExpenseHub.slnx
git push -u origin i0X-nome-da-branch
# abrir a PR -> aguardar code-quality -> ler o relatório -> corrigir -> merge
```

## 8. Checklist de entrega (até 13/10/2026)

- [ ] As 8 issues funcionais concluídas e com merge
- [ ] Testes unitários significativos passando
- [ ] Score do `code-quality` lido e registrado (meta: 100)
- [ ] README com banco, segredos, migrations e execução
- [ ] Nenhum segredo, binário ou banco versionado
- [ ] Gustavo e Leonardo com mais de um commit cada
- [ ] Repositório público e professor com acesso
- [ ] URL do repositório e SHA final da `main` enviados

## 9. Comandos úteis

```shell
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet test ./sources/ExpenseHub.slnx
dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj

dotnet tool install --global dotnet-ef
dotnet ef migrations add <Nome> --project ./sources/ExpenseHub.Api
dotnet ef database update --project ./sources/ExpenseHub.Api

dotnet user-secrets init --project ./sources/ExpenseHub.Api
dotnet user-secrets set "Seed:AdminEmail" "<email>" --project ./sources/ExpenseHub.Api
dotnet user-secrets set "Seed:AdminPassword" "<senha>" --project ./sources/ExpenseHub.Api
```

Nunca coloque os valores reais em arquivos versionados nem em prompts de IA.

## 10. Instruções para assistentes de IA

Se você é um assistente de IA ajudando um integrante do grupo:

1. Leia este plano e os documentos em `docs/` antes de propor código. O contrato
   oficial está em `docs/REQUISITOS.md` e `docs/MATRIZ-AUTORIZACAO.md`.
2. Trabalhe **apenas na issue da etapa atual** do integrante que você está
   ajudando (veja a tabela da seção 1). Não implemente issues futuras
   antecipadamente.
3. **Não altere** `.editorconfig`, `Directory.Build.props`, `scripts/`, `tests/`
   nem `.github/workflows/`, e não suprima warnings com `NoWarn` ou `#pragma`.
4. Siga as seções 3 e 4: XML doc em tipo público, usings explícitos, DTOs com
   Data Annotations, nenhum segredo em arquivo, filtro aplicado no `IQueryable`,
   regras no domínio ou serviço e um `SaveChangesAsync` por operação.
5. Proponha commits pequenos e coesos, com mensagens no padrão da seção 2. Quem
   commita é o integrante, com a própria conta.
6. Não use `Closes`, `Fixes` ou `Resolves` em PRs.
7. Testes unitários não podem depender de banco, rede ou serviço externo.
8. Explique o código gerado. O integrante precisa entender e revisar tudo o que
   entrega (veja `docs/USO-DE-IA.md`).
