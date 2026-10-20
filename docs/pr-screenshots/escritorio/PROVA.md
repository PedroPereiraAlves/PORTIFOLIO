# Prova — Escritório backend (PR A)

## Build / testes

```
dotnet test PedroHub.sln --configuration Release
→ Aprovado! Application.Tests: 87 | Web.Tests: 12 | Total: 99 (0 falha)
```

Cobertura Web.Tests (WebApplicationFactory + InMemory):

- sem header → 401
- chave errada → 401
- sem chave configurada → 503
- sem chave + JSON inválido → 401 (não 400)
- chave certa + JSON inválido / slug inexistente / status inválido → 400
- chave certa + payload válido → 204
- chave certa + corpo > 64 KB → 413
- cookie Owner sem header → 401
- 31 req/min → 429
- GET /admin/escritorio/estado anônimo → 302 login (sem JSON)

## Correções deste PR

- `GideonApiKeyFilter` agora é `IAsyncAuthorizationFilter` (auth antes do model binding)
- `GithubPrUrlRules`: host `github.com` exato + primeiro segmento do path `PedroPereiraAlves`
- `[hidden] { display: none !important; }` em `site.css`
- UI provisória = lista acessível (`a11y-list.js`); mapa fica para o PR visual
- `aria-live` anuncia só diffs de status

## Migration

```
dotnet ef database update --project src/PedroHub.Infrastructure --startup-project src/PedroHub.Web
→ Applying migration '20261020004400_AddEscritorio' (se ainda não aplicada)
```

## curl (chave fake)

Com app no ar e `HUB_GIDEON_API_KEY=chave-fake-dev` (user-secrets ou env):

```
# 204
curl -i -X POST http://localhost:5xxx/api/gideon/agentes/status \
  -H "Content-Type: application/json" -H "X-Gideon-Key: chave-fake-dev" \
  -d @docs/pr-screenshots/escritorio/status.exemplo.json

# 401 sem header
curl -i -X POST .../status -H "Content-Type: application/json" -d '{"agentes":[]}'

# 401 sem header + JSON inválido (não 400)
curl -i -X POST .../status -H "Content-Type: application/json" -d '{quebrado'

# 413
curl -i -X POST .../status -H "Content-Type: application/json" -H "X-Gideon-Key: chave-fake-dev" \
  -d "{\"agentes\":[{\"slug\":\"front-hub\",\"status\":\"trabalhando\",\"tarefaAtual\":\"$(python -c 'print(\"x\"*70000)')}\"]}"

# 400 status inválido
curl -i -X POST .../status -H "Content-Type: application/json" -H "X-Gideon-Key: chave-fake-dev" \
  -d '{"agentes":[{"slug":"front-hub","status":"voando"}]}'
```

Os mesmos cenários estão cobertos em `PedroHub.Web.Tests` (não dependem de Postgres local).

## Não testado

- Envio real pelo Gideon em produção
- Prints Playwright (mapa ainda não existe neste PR; captura fica no PR B)
- curl manual nesta sessão se Postgres/docker não estiver no ar (testes de integração cobrem os status codes)
