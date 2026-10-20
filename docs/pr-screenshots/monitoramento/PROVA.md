# Prova — Monitoramento

## Build / testes (local)

```
dotnet build PedroHub.sln
→ Compilação com êxito. 0 Aviso(s), 0 Erro(s)

dotnet test PedroHub.sln
→ Aprovado! 62 testes (0 falha)
```

## Ajustes pós-review (esta rodada)

- Confirm de Excluir via `data-confirm` + `site.js` (sem `onclick`; CSP ok).
- SSRF: `::`, NAT64 `64:ff9b::/96` e `64:ff9b:1::/48`, 6to4, IPv4-compatible, TEST-NET/benchmarking; `UseProxy = false`.
- Alertas: queda só em **Offline**; Online⇄Degradado só dashboard/log; recuperação Offline→Online/Degradado **não** é bloqueada pelo rate limit por alvo se a queda foi enviada.
- Mobile 390: `min-width: 0` no grid, histórico em cards no celular, `assert scrollWidth === 390` no capture.
- Removido do PR o commit da rule `.cursor/rules/matar-processos-ao-terminar.mdc` (fora de escopo).

## Migration

```
dotnet ef database update --project src/PedroHub.Infrastructure --startup-project src/PedroHub.Web
→ Applying migration '20261019211307_AddMonitoramento'.
→ Done.
```

## Checagem real (SITE-RMS-LOGISTICA)

Log estruturado (startup Development):

```
Checagem concluída. AlvoId=… Status=Online LatenciaMs=151 CodigoHttp=200 TraceId=…
```

Card no dashboard: **Online**, latência ~39–61 ms, uptime 100%, Infra Cloudflare Pages.
Histórico: HTTP 200, várias linhas Online.

## Prints

Pasta `docs/pr-screenshots/monitoramento/`:

- `dashboard-desktop-1440.png` (viewport 1440)
- `dashboard-desktop-dark.png` (tema escuro)
- `dashboard-mobile-390.png` (viewport **390×844**, `scrollWidth === 390`)
- `dashboard-mobile-dark-390.png` (mobile escuro, 390 reais)
- `form-desktop-1440.png`
- `form-mobile-390.png` (`scrollWidth === 390`)

CSP: console sem erros de política; Excluir mostra `confirm` nativo.

## Como testar

1. `dotnet ef database update` + `dotnet run --project src/PedroHub.Web` (Development).
2. Login Owner → `/admin/monitoramento`.
3. Seed cria SITE-RMS se ainda não existir; ou cadastre via form.
4. Clique **Checar agora** ou aguarde o BackgroundService (~15 s de ciclo, intervalo do alvo).
5. Opcional: configure `Resend:ApiKey` + `Monitoramento:Alertas:De/Para` para e-mail real.
6. Prints: `HUB_CAPTURE_EMAIL` / `HUB_CAPTURE_PASSWORD` → `node docs/pr-screenshots/capture-monitoramento.mjs`.

## Não testado

- Envio real de e-mail pelo Resend (sem API key nesta sessão — só log “alerta não enviado”).
- Redirects HTTP em produção com DNS rebinding ao vivo (coberto por testes unitários com resolvedor fake).
- Proxy de ambiente HTTP_PROXY em runtime (mitigado com `UseProxy = false`).
