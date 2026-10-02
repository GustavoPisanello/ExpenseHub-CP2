# Checkpoint 2 — ExpenseHub

Checkpoint de C# em grupos de até 3 pessoas para construção de uma Application Programming Interface (API) corporativa de reembolsos.

O prazo de entrega é **13 de outubro de 2026**. O grupo deverá implementar autenticação, autorização, fluxo de aprovação e reprovação, pagamento simulado, histórico e testes unitários.

## Criar seu repositório

1. Clique em **Use this template**.
2. Selecione **Create a new repository**.
3. Crie um repositório **público** em uma das contas do grupo.
4. Adicione os demais integrantes como colaboradores.
5. Clone o repositório.

Não use fork. As issues permanecem neste repositório original como especificação comum da turma.

Os commits serão utilizados para avaliar a participação. Todos os membros do grupo
devem possuir mais de um commit no repositório.

## Fluxo de trabalho

Para cada issue:

1. leia os critérios no repositório original;
2. crie uma branch com o identificador, por exemplo `i06-ownership`;
3. implemente e valide a feature;
4. abra uma pull request no seu próprio repositório;
5. use um título como `I06 — Ownership e matriz de acesso`;
6. adicione na descrição uma referência completa, como `Racass/checkpoint-csharpracass-expensehub#6`;
7. não use `Closes`, `Fixes` ou `Resolves`, pois a issue original deve permanecer aberta;
8. conclua a auto-revisão e faça o merge.

## Estrutura inicial

```text
sources/
├── ExpenseHub.slnx
├── ExpenseHub.Api/
└── ExpenseHub.UnitTests/
```

A solução começa sem Identity, banco, domínio ou testes funcionais. Toda implementação avaliada deve ser criada por você.

## Comandos

```shell
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet test ./sources/ExpenseHub.slnx
dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
```

O endpoint inicial `GET /health` existe apenas para confirmar que a aplicação inicia.

## Documentação

- [Enunciado](docs/ENUNCIADO.md)
- [Requisitos e contratos](docs/REQUISITOS.md)
- [Rubrica](docs/RUBRICA.md)
- [Matriz de autorização](docs/MATRIZ-AUTORIZACAO.md)
- [Processo no GitHub](docs/PROCESSO-GITHUB.md)
- [Uso de Inteligência Artificial](docs/USO-DE-IA.md)
- [Regras do pipeline de qualidade](docs/code-quality-rules.md)

## Banco de dados

### Provider e pacotes

- Provider: **SQLite**, que não precisa de servidor instalado.
- Pacote: `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12.
- Migrations: `Microsoft.EntityFrameworkCore.Design` 10.0.12 e a ferramenta local `dotnet-ef` 10.0.12, declarada em `dotnet-tools.json`.

### Configuração

A connection string fica em `sources/ExpenseHub.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "Default": "Data Source=expensehub.db"
}
```

O arquivo `expensehub.db` é criado em `sources/ExpenseHub.Api/` e não é versionado (`*.db` está no `.gitignore`). O SQLite local não usa senha.

Para usar outro caminho, sobrescreva a configuração com a variável de ambiente `ConnectionStrings__Default`.

### Criar ou atualizar o banco

A API aplica as migrations pendentes ao iniciar, então basta executá-la.

Para aplicar manualmente:

```shell
dotnet tool restore
dotnet ef database update --project ./sources/ExpenseHub.Api
```

Para criar uma nova migration depois de alterar o modelo:

```shell
dotnet ef migrations add <NomeDaMigration> --project ./sources/ExpenseHub.Api
```

Para recriar o banco do zero, apague `sources/ExpenseHub.Api/expensehub.db` e inicie a API novamente.

### Conta Admin inicial

Ao iniciar, a API cria as roles `Admin`, `Employee`, `Approver`, `Finance` e `Auditor` e uma única conta Admin. O seed é idempotente: reiniciar a aplicação não duplica roles nem usuários.

O e-mail do Admin está em `appsettings.json` (`Seed:AdminEmail`). A senha **não é versionada**: configure-a com User Secrets antes da primeira execução.

```shell
dotnet user-secrets set "Seed:AdminPassword" "<sua-senha>" --project ./sources/ExpenseHub.Api
```

A senha precisa seguir a política padrão do Identity: mínimo de 6 caracteres, com letra maiúscula, letra minúscula, número e símbolo. Fora do ambiente de desenvolvimento, use a variável de ambiente `Seed__AdminPassword`. Sem a senha configurada, a API não inicia e informa o que falta.

### Iniciar a aplicação

```shell
dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
```

Confira com `GET http://localhost:5245/health`.

## Autenticação

A API usa ASP.NET Core Identity com tokens bearer.

- `POST /login` com `{ "email": "...", "password": "..." }` retorna `accessToken`, `expiresIn` e `refreshToken`.
- Envie o token nas rotas protegidas com o cabeçalho `Authorization: Bearer <accessToken>`.
- Credenciais inválidas retornam `401`; corpo inválido retorna `400`.

As requisições de exemplo estão em `sources/ExpenseHub.Api/ExpenseHub.Api.http`.

## Usuários e roles

### Cadastro

`POST /register` é público e recebe somente `email` e `password`. O usuário é criado **sem nenhuma role**; um campo `roles` enviado pelo cliente é ignorado. E-mail duplicado, e-mail inválido ou senha fora da política retornam `400`.

### Administração (somente Admin)

| Rota | Descrição |
|---|---|
| `GET /api/admin/users` | Lista usuários com suas roles |
| `PUT /api/admin/users/{id}/roles` | Substitui o conjunto de roles do usuário. Corpo: `{ "roles": ["Employee", "Approver"] }` |

Regras do `PUT`:

- aceita apenas `Admin`, `Employee`, `Approver`, `Finance` e `Auditor` (sem diferenciar maiúsculas); qualquer outra role retorna `400` e nenhuma role nova é criada;
- `"roles": []` remove todas as roles; omitir o campo retorna `400`;
- o Admin não pode remover a própria role Admin (`400`);
- usuário inexistente retorna `404`;
- sem token retorna `401`; usuário sem a role Admin retorna `403`.

### Novo login após alterar roles

**Depois de uma alteração de roles, o usuário precisa fazer login novamente.** As roles ficam gravadas no token no momento do login, então o token antigo continua com as roles antigas até expirar (1 hora). O novo login emite um token com as roles atualizadas.

## Reembolsos

### Criar e editar rascunho (role Employee)

| Rota | Descrição |
|---|---|
| `POST /api/expenses` | Cria um reembolso em `Draft`. Retorna `201` com o recurso criado |
| `PUT /api/expenses/{id}` | Edita um rascunho próprio. Retorna `200` |

Corpo das duas rotas:

```json
{ "description": "Almoço com cliente", "amount": 120.50, "expenseDate": "2026-09-30", "categoryId": 1 }
```

| Campo | Regra |
|---|---|
| `description` | Obrigatória, de 10 a 500 caracteres |
| `amount` | De `0.01` a `2147483647` |
| `expenseDate` | Data válida (`AAAA-MM-DD`) e não futura |
| `categoryId` | Categoria existente (1 a 5, criadas pela migration) |

- Proprietário, estado, datas de sistema e identificador são definidos pelo servidor. Esses campos enviados pelo cliente são ignorados.
- Sem token: `401`. Sem a role Employee: `403`, mesmo para Admin, Approver, Finance ou Auditor.
- Editar reembolso de outro usuário ou inexistente: `404`. Editar reembolso próprio fora de `Draft`: `409`.
- A criação grava o histórico `Created`, e cada edição grava `Updated` com os campos alterados, na mesma operação.

### Enviar, listar e consultar

| Rota | Roles | Descrição |
|---|---|---|
| `POST /api/expenses/{id}/submit` | Employee | Envia um rascunho próprio: `Draft` → `Submitted` |
| `GET /api/expenses` | Employee, Approver, Finance, Auditor | Lista os reembolsos visíveis para o perfil |
| `GET /api/expenses/{id}` | Employee, Approver, Finance, Auditor | Detalhe de um reembolso visível |

Visibilidade por role (com várias roles, vale a união):

| Role | Vê |
|---|---|
| Employee | Os próprios reembolsos, em qualquer estado |
| Approver | Reembolsos `Submitted` |
| Finance | Reembolsos `Approved` e `Paid` |
| Auditor | Todos |
| Admin | Nenhum (`403`, a menos que também tenha outra role) |

- O filtro é aplicado na consulta ao banco, antes de carregar os dados.
- Reembolso inexistente ou fora da visibilidade retorna `404`, sem revelar se ele existe.
- Enviar reembolso de outro usuário: `404`. Enviar de novo ou fora de `Draft`: `409`, sem duplicar histórico.
- O envio grava o histórico `Submitted` (`Draft` → `Submitted`) na mesma operação.

## Swagger

Em ambiente de desenvolvimento, a interface do Swagger fica em `http://localhost:5245/swagger`.

1. Execute `POST /login` e copie o `accessToken` da resposta.
2. Clique em **Authorize**, cole o token e confirme.
3. As próximas requisições feitas pela interface enviam o token automaticamente.

## Testes

Somente testes unitários escritos por você entram na nota. Testes de integração, end-to-end ou de interface são permitidos, mas opcionais e sem pontuação.

Os testes unitários devem executar sem banco, rede ou serviço externo.

## Entrega

Entregue:

- URL do repositório público;
- commit Secure Hash Algorithm (SHA) final;
- integração contínua executada;
- documentação atualizada.

O projeto deve compilar sem erros e ser entregue sem warnings para receber a pontuação integral de Qualidade de Código.
