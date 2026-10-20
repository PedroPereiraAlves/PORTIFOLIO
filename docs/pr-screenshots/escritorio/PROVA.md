# Prova — Escritório virtual

## Build / testes

```
dotnet build PedroHub.sln
→ êxito, 0 avisos

dotnet test PedroHub.sln
→ Aprovado! 78 testes (0 falha)
```

## Migration

```
dotnet ef database update --project src/PedroHub.Infrastructure --startup-project src/PedroHub.Web
→ Applying migration '20261020004400_AddEscritorio'
```

## curl (chave fake)

```powershell
$env:HUB_GIDEON_API_KEY="chave-fake-dev"
dotnet run --project src/PedroHub.Web

# com chave → 204
curl.exe -i -X POST https://localhost:7240/api/gideon/agentes/status `
  -H "X-Gideon-Key: chave-fake-dev" -H "Content-Type: application/json" `
  -d "@docs/pr-screenshots/escritorio/status.exemplo.json"

# sem chave → 401
curl.exe -i -X POST https://localhost:7240/api/gideon/agentes/status `
  -H "Content-Type: application/json" `
  -d "@docs/pr-screenshots/escritorio/status.exemplo.json"
```

`GET /admin/escritorio/estado` (Owner logado) devolve agentes + últimas 5 entregas, sem IDs internos nem a chave.

## Prints

`docs/pr-screenshots/escritorio/` via `capture-escritorio.mjs` (env `HUB_CAPTURE_*`):

- desktop 1440 com painel
- dark
- mobile 390 lista e mapa (`scrollWidth === 390`)

## Não testado

- Envio real pelo Gideon em produção
- Carga / rate limit sob tráfego alto
- Prints nesta sessão (credenciais de capture não estavam no ambiente do agent)
