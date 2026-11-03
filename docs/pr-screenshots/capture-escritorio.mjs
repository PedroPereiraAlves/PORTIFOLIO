import { chromium } from "playwright";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const out = path.join(__dirname, "escritorio-v2");
const base = process.env.HUB_CAPTURE_BASE || "http://localhost:5276";
const email = process.env.HUB_CAPTURE_EMAIL;
const password = process.env.HUB_CAPTURE_PASSWORD;
const gideonKey = process.env.HUB_GIDEON_API_KEY || "chave-fake-dev";

const FAIL_RE =
  /Content Security Policy|Failed to process file|File failed|Texture key|has no frame|__MISSING|\[escritorio\] falha ao carregar/i;

if (!email || !password) {
  console.error("Defina HUB_CAPTURE_EMAIL e HUB_CAPTURE_PASSWORD.");
  process.exit(1);
}

fs.mkdirSync(out, { recursive: true });

async function assertNoHorizontalOverflow(page, expectedWidth) {
  const metrics = await page.evaluate(() => ({
    innerWidth: window.innerWidth,
    scrollWidth: document.documentElement.scrollWidth,
  }));
  if (metrics.scrollWidth !== expectedWidth) {
    throw new Error(
      `Overflow horizontal: scrollWidth=${metrics.scrollWidth} !== ${expectedWidth}`
    );
  }
  return metrics;
}

async function settle(page) {
  await page.evaluate(() => {
    document.querySelectorAll(".reveal").forEach((el) => el.classList.add("is-visible"));
  });
  await page.waitForTimeout(500);
}

async function shot(page, name, { assertWidth } = {}) {
  await settle(page);
  if (assertWidth) await assertNoHorizontalOverflow(page, assertWidth);
  const file = path.join(out, name);
  await page.screenshot({ path: file, fullPage: true });
  console.log(JSON.stringify({ name, ok: true }));
}

async function login(page) {
  await page.goto(base + "/Account/Login", { waitUntil: "networkidle" });
  await page.fill("#Email", email);
  await page.fill("#Senha", password);
  await Promise.all([
    page.waitForURL("**/admin/**"),
    page.click('button[type="submit"]'),
  ]);
}

async function seedStatuses(request) {
  const statuses = [
    ["backend-hub", "trabalhando"],
    ["front-hub", "trabalhando"],
    ["monitoramento-hub", "trabalhando"],
    ["seguranca-hub", "revisando"],
    ["infra-testes-hub", "revisando"],
    ["backend-clinica", "ocioso"],
    ["front-clinica", "ocioso"],
    ["seguranca-clinica", "ocioso"],
    ["infra-clinica", "bloqueado"],
    ["design-clinica", "pausado-sem-cota"],
    ["testes-clinica", "pausado-sem-cota"],
    ["gideon", "trabalhando"],
  ];
  for (const [slug, status] of statuses) {
    await request.post(base + "/api/gideon/agentes/status", {
      headers: {
        "Content-Type": "application/json",
        "X-Gideon-Api-Key": gideonKey,
      },
      data: {
        slug,
        status,
        tarefaAtual: status === "trabalhando" ? "Captura Playwright" : null,
        projeto: "PORTIFOLIO",
      },
    });
  }
}

async function main() {
  const consoleFails = [];
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 1,
    colorScheme: "light",
    recordVideo: { dir: out, size: { width: 960, height: 540 } },
  });
  const page = await context.newPage();
  page.on("console", (msg) => {
    const t = msg.text();
    if (FAIL_RE.test(t)) consoleFails.push(t);
  });
  page.on("pageerror", (err) => {
    const t = String(err);
    if (FAIL_RE.test(t)) consoleFails.push(t);
  });

  await page.addInitScript(() => localStorage.removeItem("pedrohung-theme"));
  await login(page);
  await seedStatuses(context.request);

  await page.goto(base + "/admin/escritorio", { waitUntil: "networkidle" });
  await page.waitForTimeout(1500);

  const loadErr = await page.locator("#escritorio-game").getAttribute("data-load-error");
  if (loadErr) {
    consoleFails.push("data-load-error=" + loadErr);
  }

  await shot(page, "mapa-desktop-1440.png");

  // Teclado: Tab Tab seleciona 2º sem abrir painel; Enter abre; Esc fecha
  const canvas = page.locator("#escritorio-game");
  await canvas.focus();
  await page.keyboard.press("Tab");
  await page.keyboard.press("Tab");
  await page.waitForTimeout(300);
  const panelStillClosed = await page.locator("#escritorioPanel").evaluate((el) => el.hidden);
  if (!panelStillClosed) {
    throw new Error("Tab abriu o painel (deveria só destacar)");
  }
  await page.keyboard.press("Enter");
  await page.waitForTimeout(400);
  const panelOpen = await page.locator("#escritorioPanel").evaluate((el) => !el.hidden);
  if (!panelOpen) throw new Error("Enter não abriu o painel");
  await shot(page, "mapa-painel-1440.png");
  await page.keyboard.press("Escape");
  await page.waitForTimeout(200);
  const focusBack = await page.evaluate(() => document.activeElement?.id === "escritorio-game");
  if (!focusBack) console.warn("aviso: foco não voltou ao mapa após Esc");

  await page.evaluate(() => {
    localStorage.setItem("pedrohung-theme", "dark");
    document.documentElement.setAttribute("data-theme", "dark");
  });
  await page.waitForTimeout(300);
  await shot(page, "mapa-dark-1440.png");

  await page.evaluate(() => {
    localStorage.removeItem("pedrohung-theme");
    document.documentElement.setAttribute("data-theme", "light");
  });

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(base + "/admin/escritorio", { waitUntil: "networkidle" });
  await page.waitForTimeout(600);
  await shot(page, "lista-mobile-390.png", { assertWidth: 390 });

  const toggle = page.locator("#escritorioToggleMapLista");
  if ((await toggle.count()) > 0) {
    await toggle.click();
    await page.waitForTimeout(1200);
    await shot(page, "mapa-mobile-390.png", { assertWidth: 390 });
  }

  if (consoleFails.length) {
    console.error("Falhas de carga/CSP:", consoleFails);
    await browser.close();
    process.exit(1);
  }
  console.log("Console CSP/load: ok");
  await context.close();
  await browser.close();

  // Renomeia vídeo Playwright → demo.webm se existir
  const videos = fs.readdirSync(out).filter((f) => f.endsWith(".webm"));
  if (videos.length) {
    const dest = path.join(out, "demo.webm");
    fs.renameSync(path.join(out, videos[0]), dest);
    console.log("demo.webm gravado (converte para GIF com ffmpeg se quiser)");
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
