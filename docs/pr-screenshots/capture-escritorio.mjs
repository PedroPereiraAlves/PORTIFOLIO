import { chromium } from "playwright";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const out = path.join(__dirname, "escritorio-v2");
const base = process.env.HUB_CAPTURE_BASE || "http://localhost:5276";
const email = process.env.HUB_CAPTURE_EMAIL;
const password = process.env.HUB_CAPTURE_PASSWORD;

if (!email || !password) {
  console.error("Defina HUB_CAPTURE_EMAIL e HUB_CAPTURE_PASSWORD. Ver README.");
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
  const box = await page.evaluate(() => ({
    w: window.innerWidth,
    scrollWidth: document.documentElement.scrollWidth,
    theme: document.documentElement.getAttribute("data-theme"),
  }));
  console.log(JSON.stringify({ name, ...box }));
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

async function main() {
  const consoleErrors = [];
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 1,
    colorScheme: "light",
  });
  const page = await context.newPage();
  page.on("console", (msg) => {
    const t = msg.text();
    if (/Content Security Policy|CSP|Refused to|error/i.test(t) && msg.type() === "error") {
      consoleErrors.push(t);
    }
  });
  page.on("pageerror", (err) => consoleErrors.push(String(err)));

  await page.addInitScript(() => localStorage.removeItem("pedrohung-theme"));
  await login(page);

  await page.goto(base + "/admin/escritorio", { waitUntil: "networkidle" });
  await page.waitForTimeout(1200);
  await shot(page, "mapa-desktop-1440.png");

  // Teclado: foco no mapa + Enter abre painel
  const canvas = page.locator("#escritorio-game");
  await canvas.focus();
  await page.keyboard.press("Tab");
  await page.keyboard.press("Enter");
  await page.waitForTimeout(500);
  const panelOpen = await page.locator("#escritorioPanel").evaluate((el) => !el.hidden);
  if (!panelOpen) {
    // fallback lista
    const row = page.locator("#escritorioTabela tbody tr").first();
    if ((await row.count()) > 0) await row.click();
  }
  await page.waitForTimeout(400);
  await shot(page, "mapa-painel-1440.png");

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

  const toggle = page.locator("#escritorioToggleView");
  if ((await toggle.count()) > 0) {
    await toggle.click();
    await page.waitForTimeout(600);
    await shot(page, "mapa-mobile-390.png", { assertWidth: 390 });
  }

  if (consoleErrors.length) {
    console.error("Console errors:", consoleErrors);
    process.exitCode = 1;
  } else {
    console.log("Console errors: none");
  }
  await browser.close();
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
