import { chromium } from "playwright";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const out = __dirname;
const base = "http://localhost:5276";

async function revealAll(page) {
  await page.evaluate(() => {
    document.querySelectorAll(".reveal").forEach((el) => el.classList.add("is-visible"));
  });
}

async function shot(page, name) {
  await revealAll(page);
  await page.waitForTimeout(150);
  const file = path.join(out, name);
  await page.screenshot({ path: file, fullPage: true });
  const box = await page.evaluate(() => ({
    w: window.innerWidth,
    h: window.innerHeight,
    docH: document.documentElement.scrollHeight,
    hasDestaque: !!document.querySelector(".badge-gold"),
    projects: document.querySelectorAll(".project-card").length,
    theme: document.documentElement.getAttribute("data-theme"),
    htmlClass: document.documentElement.className,
  }));
  console.log(JSON.stringify({ name, ...box }));
}

async function login(page) {
  await page.goto(base + "/Account/Login", { waitUntil: "networkidle" });
  if (!page.url().includes("Login")) return;
  await page.fill("#Email", "owner@local.test");
  await page.fill("#Senha", "OwnerTest1!");
  await Promise.all([
    page.waitForURL("**/admin/**"),
    page.click('button[type="submit"]'),
  ]);
  await page.waitForLoadState("networkidle");
}

async function logout(page) {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(base + "/", { waitUntil: "networkidle" });
  const btn = page.locator(".nav-logout-btn");
  if ((await btn.count()) > 0) {
    await btn.click();
    await page.waitForLoadState("networkidle");
  }
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 1,
    colorScheme: "light",
  });
  await context.addInitScript(() => {
    localStorage.removeItem("pedrohung-theme");
  });
  const page = await context.newPage();

  // Login, mark destaque, capture owner header + admin
  await login(page);
  await page.goto(base + "/admin/projetos", { waitUntil: "networkidle" });
  const destacar = page.locator('form[action*="AlternarDestaque"] button', {
    hasText: /^Destacar$/,
  });
  if ((await destacar.count()) > 0) {
    await Promise.all([page.waitForLoadState("networkidle"), destacar.first().click()]);
  }
  await page.goto(base + "/admin/projetos", { waitUntil: "networkidle" });
  await shot(page, "admin-desktop-1440.png");
  await page.screenshot({
    path: path.join(out, "header-owner-1440.png"),
    clip: { x: 0, y: 0, width: 1440, height: 72 },
  });
  console.log("header-owner-1440.png clipped");

  await page.goto(base + "/admin/projetos/novo", { waitUntil: "networkidle" });
  await shot(page, "form-desktop-1440.png");

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(base + "/admin/projetos", { waitUntil: "networkidle" });
  await shot(page, "admin-mobile-390.png");
  await page.goto(base + "/admin/projetos/novo", { waitUntil: "networkidle" });
  await shot(page, "form-mobile-390.png");

  // Public pages (logged out)
  await logout(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(base + "/", { waitUntil: "networkidle" });
  await page.evaluate(() => document.documentElement.setAttribute("data-theme", "light"));
  await shot(page, "home-desktop-1440.png");

  await page.evaluate(() => {
    localStorage.setItem("pedrohung-theme", "dark");
    document.documentElement.setAttribute("data-theme", "dark");
  });
  await shot(page, "home-desktop-dark.png");

  await page.evaluate(() => {
    localStorage.removeItem("pedrohung-theme");
    document.documentElement.setAttribute("data-theme", "light");
  });
  await page.goto(base + "/Account/Login", { waitUntil: "networkidle" });
  await shot(page, "login-desktop-1440.png");

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(base + "/", { waitUntil: "networkidle" });
  await page.evaluate(() => document.documentElement.setAttribute("data-theme", "light"));
  await shot(page, "home-mobile-390.png");

  await page.click("#navToggle");
  await page.waitForTimeout(250);
  await shot(page, "home-mobile-menu-390.png");

  await page.goto(base + "/Account/Login", { waitUntil: "networkidle" });
  await shot(page, "login-mobile-390.png");

  // CSP console check
  const cspErrors = [];
  page.on("console", (msg) => {
    const t = msg.text();
    if (/Content Security Policy|CSP|Refused to execute/i.test(t)) cspErrors.push(t);
  });
  page.on("pageerror", (err) => cspErrors.push(String(err)));
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(base + "/", { waitUntil: "networkidle" });
  await page.goto(base + "/Account/Login", { waitUntil: "networkidle" });
  await login(page);
  await page.goto(base + "/admin/projetos", { waitUntil: "networkidle" });
  console.log("CSP errors:", cspErrors.length ? cspErrors : "none");
  console.log("html has js class:", await page.evaluate(() => document.documentElement.classList.contains("js")));

  await browser.close();
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
