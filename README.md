# Hub de Projetos (PedroHub)

Portfólio público de **Pedro Pereira Alves** (desenvolvedor back-end .NET) com área privada do Owner.

## Stack

- .NET 10 / ASP.NET Core MVC + Razor
- Clean Architecture (Domain, Application, Infrastructure, Web, Shared)
- CQRS com MediatR + FluentValidation + `Result<T>`
- EF Core + PostgreSQL (Npgsql)
- Cookie auth (HttpOnly, SameSite=Strict; Secure sempre fora de Development)
- Serilog, `/health`, CSP, HSTS, rate limit, ForwardedHeaders (proxy/Render)
- Sync de stars do GitHub (`HUB_GITHUB_TOKEN`)

## Pré-requisitos (Windows)

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (opcional, recomendado)
- PostgreSQL 16+ (se não usar Compose)
- Ferramenta EF: `dotnet tool install --global dotnet-ef`

## Variáveis de ambiente (obrigatórias)

Copie `.env.example` → `.env` e preencha **todos** os valores (nunca commite o `.env`; ele já está no `.gitignore`).

| Variável | Uso |
|---|---|
| `HUB_OWNER_EMAIL` | E-mail do Owner (seed; sem registro público) |
| `HUB_OWNER_PASSWORD` | Senha forte do Owner (seed) |
| `HUB_GITHUB_TOKEN` | Token GitHub para `GET /user/starred` |
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | Credenciais do Postgres no Compose |
| `ConnectionStrings__Default` | Connection string (local / `dotnet ef` / produção) |

Não há senhas padrão no código nem fallbacks inseguros no Compose: se faltar variável, o Compose falha com mensagem explícita.

Connection string **não** fica em `appsettings.json` (base). Em Development use `appsettings.Development.json`, user-secrets ou env.

## Subir com Docker Compose

```powershell
copy .env.example .env
# edite .env — todas as variáveis obrigatórias
docker compose up --build
```

App em http://localhost:8080 — health em http://localhost:8080/health

## Rodar localmente (sem Docker da app)

1. Suba o Postgres (Compose só do `db` ou instalação local).

2. Configure Owner/token (env) e a connection string. Preferência: **user-secrets no Web** (sem exportar `$env:ConnectionStrings__Default`):

```powershell
$env:HUB_OWNER_EMAIL="seu-email@exemplo.com"
$env:HUB_OWNER_PASSWORD="sua-senha-forte"
$env:HUB_GITHUB_TOKEN="ghp_..."

dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=pedrohung;Username=pedrohung;Password=sua-senha-postgres" --project src/PedroHub.Web
```

Alternativa: `$env:ConnectionStrings__Default="..."` ou `appsettings.Development.json` (não use o `appsettings.json` base para senhas).

3. Restore, migrate e run — o `dotnet ef` com `--startup-project src/PedroHub.Web` lê env, depois appsettings do Web e **user-secrets**:

```powershell
dotnet restore PedroHub.sln
dotnet ef database update --project src/PedroHub.Infrastructure --startup-project src/PedroHub.Web
dotnet run --project src/PedroHub.Web
```

## Testes

```powershell
dotnet test PedroHub.sln
```

## Estrutura

```
src/
  PedroHub.Domain/
  PedroHub.Application/
  PedroHub.Infrastructure/
  PedroHub.Web/
  PedroHub.Shared/
tests/
  PedroHub.Application.Tests/
```

## Rotas principais

| Rota | Acesso |
|---|---|
| `/` | Público — hero, sobre, experiência, competências, projetos |
| `/Account/Login` | Público — login Owner |
| `/admin` | Role `Owner` — CRUD leve de projetos + sync |
| `/admin/saude`, `/admin/cursor`, `/admin/gideon` | Owner — “em breve” |
| `/health` | Anônimo |

## Fora do escopo (Fase 1)

Monitoramento de saúde com alertas, consumo Cursor, Hangfire/Rotinas, Gideon completo, auditoria sob demanda, escritório virtual.
