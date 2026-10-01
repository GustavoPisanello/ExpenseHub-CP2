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
