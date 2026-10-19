import { chromium } from "playwright";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const out = path.join(__dirname, "monitoramento");
const base = process.env.HUB_CAPTURE_BASE || "http://localhost:5276";
const email = process.env.HUB_CAPTURE_EMAIL;
const password = process.env.HUB_CAPTURE_PASSWORD;

if (!email || !password) {
  console.error("Defina HUB_CAPTURE_EMAIL e HUB_CAPTURE_PASSWORD. Ver README.");
  process.exit(1);
}

fs.mkdirSync(out, { recursive: true });

async function settle(page) {
  await page.evaluate(() => {
    document.querySelectorAll(".reveal").forEach((el) => el.classList.add("is-visible"));
  });
  await page.evaluate(async () => {
    window.scrollTo(0, document.body.scrollHeight);
    await new Promise((r) => setTimeout(r, 250));
    window.scrollTo(0, 0);
    await new Promise((r) => setTimeout(r, 400));
  });
  await page.waitForTimeout(250);
}

async function shot(page, name) {
  await settle(page);
  const file = path.join(out, name);
  await page.screenshot({ path: file, fullPage: true });
  const box = await page.evaluate(() => ({
    w: window.innerWidth,
    h: window.innerHeight,
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
  await page.waitForLoadState("networkidle");
}

async function main() {
  const cspErrors = [];
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 1,
    colorScheme: "light",
  });
  const page = await context.newPage();
  page.on("console", (msg) => {
    const t = msg.text();
    if (/Content Security Policy|CSP|Refused to execute/i.test(t)) cspErrors.push(t);
  });
  page.on("pageerror", (err) => cspErrors.push(String(err)));

  await page.addInitScript(() => localStorage.removeItem("pedrohung-theme"));
  await login(page);

  // Checar agora se houver botão
  await page.goto(base + "/admin/monitoramento", { waitUntil: "networkidle" });
  const checar = page.locator('form[action*="/checar"] button');
  if ((await checar.count()) > 0) {
    await Promise.all([page.waitForLoadState("networkidle"), checar.first().click()]);
    await page.waitForTimeout(500);
  }

  await page.goto(base + "/admin/monitoramento", { waitUntil: "networkidle" });
  await page.waitForSelector("#grafico-latencia, .state-empty", { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(600);
  await shot(page, "dashboard-desktop-1440.png");

  // Tema escuro sem reload (evita race com theme-init)
  await page.evaluate(() => {
    localStorage.setItem("pedrohung-theme", "dark");
    document.documentElement.setAttribute("data-theme", "dark");
  });
  await page.waitForTimeout(400);
  await shot(page, "dashboard-desktop-dark.png");

  await page.evaluate(() => {
    localStorage.removeItem("pedrohung-theme");
    document.documentElement.setAttribute("data-theme", "light");
  });

  await page.goto(base + "/admin/monitoramento/novo", { waitUntil: "networkidle" });
  await shot(page, "form-desktop-1440.png");

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(base + "/admin/monitoramento", { waitUntil: "networkidle" });
  await page.waitForTimeout(600);
  await shot(page, "dashboard-mobile-390.png");

  await page.goto(base + "/admin/monitoramento/novo", { waitUntil: "networkidle" });
  await shot(page, "form-mobile-390.png");

  console.log("CSP errors:", cspErrors.length ? cspErrors : "none");
  await browser.close();
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
