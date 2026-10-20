# Prova — Escritório backend (PR A)

## Build / testes

```
dotnet test PedroHub.sln --configuration Release
→ Aprovado! Application.Tests: 87 | Web.Tests: 13 | Total: 100 (0 falha)
```

Cobertura Web.Tests (WebApplicationFactory + InMemory, `Database:SkipMigrate=true` + `EnsureCreated` na factory):

- sem header → 401
- chave errada → 401
- sem chave configurada → 503
- sem chave + JSON inválido → 401 (não 400)
- chave certa + JSON inválido / slug inexistente / status inválido → 400
- chave certa + payload válido → 204
- chave certa + corpo > 64 KB (Content-Length) → 413
- chave certa + corpo chunked > 64 KB (sem Content-Length) → 413
- cookie Owner sem header → 401
- 31 req/min → 429
- GET /admin/escritorio/estado anônimo → 302 login (sem JSON)

## Correções deste PR

- `GideonApiKeyFilter` → `IAsyncAuthorizationFilter` (auth antes do model binding)
- `GideonBodySizeFilter` → buffer com teto 64 KB (Content-Length e chunked)
- `GithubPrUrlRules`: host `github.com` + primeiro segmento `PedroPereiraAlves`
- `Program.cs`: só `MigrateAsync()`; testes usam `Database:SkipMigrate=true`
- `[hidden] { display: none !important; }` em `site.css`
- UI provisória = lista acessível (`a11y-list.js`); mapa no PR visual
- `aria-live` anuncia só diffs de status

## Migration

```
dotnet ef database update --project src/PedroHub.Infrastructure --startup-project src/PedroHub.Web
→ Applying migration '20261020004400_AddEscritorio' (se ainda não aplicada)
```

## Prints da lista

```
docs/pr-screenshots/escritorio/lista-desktop-1440.png
docs/pr-screenshots/escritorio/lista-mobile-390.png
```

Gerar com Owner logado + `capture-escritorio.mjs` (ou print manual em `/admin/escritorio`).  
Se `HUB_CAPTURE_*` não estiver no ambiente do agent, o Pedro roda localmente (ver PR B / README).

## curl (chave fake)

Com app no ar e `HUB_GIDEON_API_KEY=chave-fake-dev`:

```
# 204 / 401 / 401+JSON inválido / 413 / 400 — mesmos casos dos Web.Tests
```

## Não testado

- Envio real pelo Gideon em produção
- Prints da lista nesta sessão se credenciais/Postgres ausentes
- CI run URL: cole aqui após abrir o PR (workflow só dispara em `pull_request`)
