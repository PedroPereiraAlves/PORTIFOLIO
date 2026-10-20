## O quê

Módulo de **Monitoramento** na área privada do Owner: alvos HTTP (GET/HEAD), checagem periódica com anti-flapping, histórico, uptime 24h/7d/30d, p50/p95, gráfico de latência (Chart.js local), CRUD admin e alertas Resend opcionais.

Commits (em ordem): domínio → persistence/migration → Application → checker/SSRF/alertas/seed → UI → testes → prints → ajustes de review (CSP, SSRF, alertas, mobile 390).

## Por quê

Substituir o placeholder “Monitoramento · em breve” por saúde real dos projetos (começando pelo SITE-RMS-LOGISTICA), com regras 100% no servidor e proteção SSRF — sem secrets no repositório.

## Como funciona (resumo)

- **Status:** esperado + latência ≤ limiar → Online; esperado + latência alta → Degradado; falha 1× → Degradado; ≥2 falhas consecutivas → Offline (`Monitoramento:FalhasParaOffline`).
- **Uptime:** Online **e Degradado** contam como up (serviço respondeu no status esperado; só estava lento).
- **Alertas:** queda só quando chega em **Offline**; Online ⇄ Degradado fica no dashboard/log. Recuperação (Offline → Online/Degradado) **não** é engolida pelo rate limit por alvo se a queda correspondente foi enviada. Duas quedas seguidas dentro da janela respeitam o rate limit. Sem API key → só log.
- **SSRF:** bloqueia loopback, privados, metadata, `::`, NAT64, 6to4, IPv4-compatible, TEST-NET/benchmarking; handler com `UseProxy = false` e connect só nos IPs validados.
- **Seed Dev:** SITE-RMS-LOGISTICA em `https://site-rms-logistica.pages.dev/`, vínculo opcional ao Projeto GitHub.

## Como testar

1. `dotnet ef database update --project src/PedroHub.Infrastructure --startup-project src/PedroHub.Web`
2. `dotnet run --project src/PedroHub.Web` (Development)
3. Login Owner → `/admin/monitoramento` (ou `/admin/saude`, redireciona)
4. **Checar agora** ou aguardar o BackgroundService
5. Opcional alertas: user-secrets `Resend:ApiKey`, `Monitoramento:Alertas:De`, `Monitoramento:Alertas:Para`
6. Excluir alvo: deve pedir confirmação (sem erro de CSP no console)

```
dotnet build PedroHub.sln   # êxito, 0 avisos
dotnet test PedroHub.sln    # 62 aprovados
```

Prints: `docs/pr-screenshots/monitoramento/` (desktop 1440, mobile **390 reais** com assert de `scrollWidth`, tema escuro, form). Detalhes em `docs/pr-screenshots/monitoramento/PROVA.md`.

Captura Playwright: `HUB_CAPTURE_EMAIL` / `HUB_CAPTURE_PASSWORD` (ver README) — credenciais removidas do script.

## Riscos

- Checker faz HTTP de saída; SSRF mitiga, mas DNS/rede errados ainda geram falhas/timeouts.
- Free-tier / cold start do alvo pode ocasionar Degradado por latência sem ser Offline (e sem e-mail — só Offline alerta).
- Sem Resend configurado não há e-mail real (só log).
- Chart.js vendored em `wwwroot/lib/chartjs/` (atualização manual).

## Provas

- Migration `20261019211307_AddMonitoramento` aplicada com sucesso.
- Checagem real: `Status=Online LatenciaMs≈40–150 CodigoHttp=200` (log + card + histórico nos prints).
- CSP: sem erros de script no console; confirm de Excluir via JS externo.
- Mobile: `document.documentElement.scrollWidth === 390` no capture.
- **Não testado:** envio real Resend (sem API key nesta sessão); HTTP_PROXY em runtime.

## Fora de escopo

WhatsApp/Telegram, Consumo Cursor, Gideon, tela completa de Rotinas/Hangfire, Identity/login além do necessário.
