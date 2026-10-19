# Prova — Monitoramento

## Build / testes (local)

```
dotnet build PedroHub.sln
→ Compilação com êxito. 0 Aviso(s), 0 Erro(s)

dotnet test PedroHub.sln
→ Aprovado! 40 testes (0 falha)
```

## Migration

```
dotnet ef database update --project src/PedroHub.Infrastructure --startup-project src/PedroHub.Web
→ Applying migration '20261019211307_AddMonitoramento'.
→ Done.
```

## Checagem real (SITE-RMS-LOGISTICA)

Log estruturado (startup Development):

```
Checagem concluída. AlvoId=… Status=Online LatenciaMs=151 CodigoHttp=200
```

Card no dashboard: **Online**, latência ~39–61 ms, uptime 100%, Infra Cloudflare Pages.
Histórico: HTTP 200, várias linhas Online.

## Prints

Pasta `docs/pr-screenshots/monitoramento/`:

- `dashboard-desktop-1440.png` (viewport 1440)
- `dashboard-desktop-dark.png` (tema escuro)
- `dashboard-mobile-390.png` (viewport **390×844**)
- `form-desktop-1440.png`
- `form-mobile-390.png`

CSP: console sem erros de política.

## Como testar

1. `dotnet ef database update` + `dotnet run --project src/PedroHub.Web` (Development).
2. Login Owner → `/admin/monitoramento`.
3. Seed cria SITE-RMS se ainda não existir; ou cadastre via form.
4. Clique **Checar agora** ou aguarde o BackgroundService (~15 s de ciclo, intervalo do alvo).
5. Opcional: configure `Resend:ApiKey` + `Monitoramento:Alertas:De/Para` para e-mail real.

## Não testado

- Envio real de e-mail pelo Resend (sem API key nesta sessão — só log “alerta não enviado”).
- Redirects HTTP em produção com DNS rebinding ao vivo (coberto por testes unitários com resolvedor fake).
