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
| `/admin/monitoramento` | Role `Owner` — alvos, uptime, histórico e checagem |
| `/admin/saude` | Redireciona para `/admin/monitoramento` |
| `/admin/escritorio` | Role `Owner` — mapa pixel art dos agentes (estado via Gideon) |
| `/api/gideon/agentes/status` | Anônimo + header `X-Gideon-Key` — ingestão de status |
| `/api/gideon/agentes/eventos` | Anônimo + header `X-Gideon-Key` — feed de entregas |
| `/admin/cursor`, `/admin/gideon` | Owner — “em breve” |
| `/health` | Anônimo |

## Monitoramento (alertas opcionais)

Configure via user-secrets ou env (nunca no repositório):

```powershell
dotnet user-secrets set "Resend:ApiKey" "re_..." --project src/PedroHub.Web
dotnet user-secrets set "Monitoramento:Alertas:De" "alertas@seudominio.com" --project src/PedroHub.Web
dotnet user-secrets set "Monitoramento:Alertas:Para" "voce@email.com" --project src/PedroHub.Web
```

Sem `Resend:ApiKey`, o checker segue normalmente e só registra log (“alerta não enviado”).

## Escritório virtual (ingestão Gideon)

O Hub **não** chama o Gideon; só recebe status/eventos. Configure a chave (nunca no repo):

```powershell
# env
$env:HUB_GIDEON_API_KEY="chave-fake-dev"

# ou user-secrets
dotnet user-secrets set "Gideon:ApiKey" "chave-fake-dev" --project src/PedroHub.Web
dotnet user-secrets set "Escritorio:RetencaoDias" "90" --project src/PedroHub.Web
dotnet user-secrets set "Escritorio:MinutosDesatualizado" "30" --project src/PedroHub.Web
dotnet user-secrets set "Escritorio:PollingSegundos" "20" --project src/PedroHub.Web
```

Exemplo local (após `dotnet run`):

```powershell
curl.exe -i -X POST https://localhost:7240/api/gideon/agentes/status `
  -H "X-Gideon-Key: chave-fake-dev" `
  -H "Content-Type: application/json" `
  -d "@docs/pr-screenshots/escritorio/status.exemplo.json"
```

Sem header ou chave errada → **401**. Sem chave configurada no servidor → **503**.

## Prints Playwright

Credenciais **não** ficam no script. Exporte e rode:

```powershell
$env:HUB_CAPTURE_EMAIL="seu-owner@local"
$env:HUB_CAPTURE_PASSWORD="sua-senha"
# opcional: $env:HUB_CAPTURE_BASE="http://localhost:5276"
node docs/pr-screenshots/capture.mjs
node docs/pr-screenshots/capture-monitoramento.mjs
node docs/pr-screenshots/capture-escritorio.mjs
```

Saídas em `docs/pr-screenshots/`, `monitoramento/` e `escritorio/`. Viewport mobile real: **390×844**.

## Fora do escopo (próximas fases)

Consumo Cursor, Hangfire/Rotinas (tela completa), chat/comandos Hub→Gideon, Cérebro do Gideon, WhatsApp/Telegram.
