# Hub de Projetos (PedroHub)

Portfólio público de **Pedro Pereira Alves** (desenvolvedor back-end .NET) com área privada do Owner.

## Stack

- .NET 10 / ASP.NET Core MVC + Razor
- Clean Architecture (Domain, Application, Infrastructure, Web, Shared)
- CQRS com MediatR + FluentValidation + `Result<T>`
- EF Core + PostgreSQL (Npgsql)
- Cookie auth (HttpOnly, SameSite=Strict, Secure em HTTPS)
- Serilog, `/health`, CSP, HSTS, rate limit
- Sync de stars do GitHub (`HUB_GITHUB_TOKEN`)

## Pré-requisitos (Windows)

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (opcional, recomendado)
- PostgreSQL 16+ (se não usar Compose)
- Ferramenta EF: `dotnet tool install --global dotnet-ef`

## Variáveis de ambiente

Copie `.env.example` e ajuste (nunca commite segredos):

| Variável | Uso |
|---|---|
| `HUB_OWNER_EMAIL` | E-mail do Owner (seed) |
| `HUB_OWNER_PASSWORD` | Senha do Owner (seed) |
| `HUB_GITHUB_TOKEN` | Token GitHub para `/user/starred` |
| `ConnectionStrings__Default` | Connection string Postgres |

## Subir com Docker Compose

```powershell
copy .env.example .env
# edite .env com e-mail/senha/token
docker compose up --build
```

App em http://localhost:8080 — health em http://localhost:8080/health

## Rodar localmente (sem Docker da app)

1. Suba só o Postgres:

```powershell
docker compose up -d db
```

2. Configure variáveis na sessão:

```powershell
$env:HUB_OWNER_EMAIL="owner@local.test"
$env:HUB_OWNER_PASSWORD="Owner123!"
$env:HUB_GITHUB_TOKEN="ghp_..."
$env:ConnectionStrings__Default="Host=localhost;Port=5432;Database=pedrohung;Username=pedrohung;Password=pedrohung"
```

3. Restore, migrate e run:

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
