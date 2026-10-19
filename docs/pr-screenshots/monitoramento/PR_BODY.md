## O quê

Módulo de **Monitoramento** na área privada do Owner: alvos HTTP (GET/HEAD), checagem periódica com anti-flapping, histórico, uptime 24h/7d/30d, p50/p95, gráfico de latência (Chart.js local), CRUD admin e alertas Resend opcionais.

Commits (em ordem): domínio → persistence/migration → Application → checker/SSRF/alertas/seed → UI → testes → prints.

## Por quê

Substituir o placeholder “Monitoramento · em breve” por saúde real dos projetos (começando pelo SITE-RMS-LOGISTICA), com regras 100% no servidor e proteção SSRF — sem secrets no repositório.

## Como funciona (resumo)

- **Status:** esperado + latência ≤ limiar → Online; esperado + latência alta → Degradado; falha 1× → Degradado; ≥2 falhas consecutivas → Offline (`Monitoramento:FalhasParaOffline`).
- **Uptime:** Online **e Degradado** contam como up (serviço respondeu no status esperado; só estava lento).
- **Alertas:** só na mudança Online ⇄ Offline/Degradado (ignora saída de Desconhecido); rate limit; sem API key → só log.
- **Seed Dev:** SITE-RMS-LOGISTICA em `https://site-rms-logistica.pages.dev/`, vínculo opcional ao Projeto GitHub.

## Como testar

1. `dotnet ef database update --project src/PedroHub.Infrastructure --startup-project src/PedroHub.Web`
2. `dotnet run --project src/PedroHub.Web` (Development)
3. Login Owner → `/admin/monitoramento` (ou `/admin/saude`, redireciona)
4. **Checar agora** ou aguardar o BackgroundService
5. Opcional alertas: user-secrets `Resend:ApiKey`, `Monitoramento:Alertas:De`, `Monitoramento:Alertas:Para`

```
dotnet build PedroHub.sln   # êxito, 0 avisos
dotnet test PedroHub.sln    # 40 aprovados
```

Prints: `docs/pr-screenshots/monitoramento/` (desktop 1440, mobile **390**, tema escuro, form). Detalhes em `docs/pr-screenshots/monitoramento/PROVA.md`.

Captura Playwright: `HUB_CAPTURE_EMAIL` / `HUB_CAPTURE_PASSWORD` (ver README) — credenciais removidas do script.

## Riscos

- Checker faz HTTP de saída; SSRF mitiga, mas DNS/rede errados ainda geram falhas/timeouts.
- Free-tier / cold start do alvo pode ocasionar Degradado por latência sem ser Offline.
- Sem Resend configurado não há e-mail real (só log).
- Chart.js vendored em `wwwroot/lib/chartjs/` (atualização manual).

## Provas

- Migration `20261019211307_AddMonitoramento` aplicada com sucesso.
- Checagem real: `Status=Online LatenciaMs≈40–150 CodigoHttp=200` (log + card + histórico nos prints).
- CSP: sem erros de script no console nos prints.
- **Não testado:** envio real Resend (sem API key nesta sessão).

## Fora de escopo

WhatsApp/Telegram, Consumo Cursor, Gideon, tela completa de Rotinas/Hangfire, Identity/login além do necessário.
