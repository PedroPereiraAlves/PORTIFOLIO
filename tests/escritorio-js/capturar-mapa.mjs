/**
 * Prints do mapa compacto. Sobe o mesmo harness do teste de teclado.
 * Uso: node tests/escritorio-js/capturar-mapa.mjs
 */
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import { startHarness } from "./harness.mjs";

const require = createRequire(
  path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../docs/pr-screenshots/package.json")
);
const { chromium } = require("playwright");

const outDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../docs/pr-screenshots/escritorio-v2"
);

async function mapaPronto(page) {
  await page.waitForSelector("#escritorio-game canvas", { timeout: 15000 });
  await page.waitForFunction(() => {
    const el = document.querySelector("#escritorio-game");
    return el && !el.getAttribute("data-load-error") && el.querySelector("canvas");
  });
  await page.waitForTimeout(1200);
}

async function main() {
  const { server, url } = await startHarness();
  const browser = await chromium.launch({ headless: true });
  try {
    const desktop = await browser.newPage({
      viewport: { width: 1280, height: 800 },
      deviceScaleFactor: 1,
      colorScheme: "light",
    });
    await desktop.goto(url, { waitUntil: "networkidle" });
    await mapaPronto(desktop);
    await desktop.screenshot({
      path: path.join(outDir, "mapa-desktop-1280.png"),
      fullPage: true,
    });
    console.log("mapa-desktop-1280.png");

    const mapa = desktop.locator("#escritorio-game");
    await mapa.focus();
    await desktop.keyboard.press("Tab");
    await desktop.keyboard.press("Enter");
    await desktop.waitForTimeout(400);
    const aberto = await desktop.locator("#escritorioPanel").evaluate((el) => !el.hidden);
    if (!aberto) throw new Error("painel não abriu por teclado");
    await desktop.screenshot({
      path: path.join(outDir, "mapa-painel-teclado-1280.png"),
      fullPage: true,
    });
    console.log("mapa-painel-teclado-1280.png");

    await desktop.keyboard.press("Escape");
    await desktop.waitForTimeout(150);
    await desktop.evaluate(() => {
      document.documentElement.setAttribute("data-theme", "dark");
    });
    await desktop.waitForTimeout(300);
    await desktop.screenshot({
      path: path.join(outDir, "mapa-dark-1280.png"),
      fullPage: true,
    });
    console.log("mapa-dark-1280.png");
    await desktop.close();

    const mobile = await browser.newPage({
      viewport: { width: 390, height: 844 },
      deviceScaleFactor: 1,
      colorScheme: "light",
    });
    await mobile.goto(url, { waitUntil: "networkidle" });
    await mobile.waitForTimeout(400);
    const toggle = mobile.locator("#escritorioToggleMapLista");
    await toggle.click();
    await mapaPronto(mobile);
    const largura = await mobile.evaluate(() => document.documentElement.scrollWidth);
    if (largura > 390) {
      throw new Error("overflow horizontal em 390px: scrollWidth=" + largura);
    }
    await mobile.screenshot({
      path: path.join(outDir, "mapa-mobile-390.png"),
      fullPage: true,
    });
    console.log("mapa-mobile-390.png", "scrollWidth", largura);
    await mobile.close();
  } finally {
    await browser.close();
    await new Promise((resolve) => server.close(resolve));
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
