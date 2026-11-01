import test from "node:test";
import assert from "node:assert/strict";
import { createRequire } from "node:module";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { startHarness } from "./harness.mjs";

const require = createRequire(
  path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../docs/pr-screenshots/package.json")
);
const { chromium } = require("playwright");

async function painelVisivel(page) {
  return page.locator("#escritorioPanel").evaluate((el) => !el.hidden && el.classList.contains("is-open"));
}

test("Tab+Enter no mapa e Enter na lista deixam o painel aberto", async () => {
  const { server, url } = await startHarness();
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  const falhas = [];
  page.on("console", (msg) => {
    const t = msg.text();
    if (/falha ao carregar|data-load-error|Texture key/i.test(t)) falhas.push(t);
  });
  page.on("pageerror", (err) => falhas.push(String(err)));

  try {
    await page.goto(url, { waitUntil: "networkidle" });
    await page.waitForSelector("#escritorio-game canvas", { timeout: 15000 });
    await page.waitForFunction(() => document.querySelectorAll("#escritorioTabela tbody tr").length >= 12);
    const loadErr = await page.locator("#escritorio-game").getAttribute("data-load-error");
    assert.equal(loadErr, null, "mapa não carregou: " + loadErr);

    const mapa = page.locator("#escritorio-game");
    await mapa.focus();
    await page.keyboard.press("Tab");
    await page.waitForTimeout(50);
    assert.equal(await painelVisivel(page), false, "Tab não deve abrir o painel");
    await page.keyboard.press("Enter");
    await page.waitForTimeout(250);
    assert.equal(await painelVisivel(page), true, "Enter no mapa deve deixar #escritorioPanel visível");
    const titulo = await page.locator("#escritorioPanelTitle").textContent();
    assert.match(titulo || "", /Gideon/);

    await page.keyboard.press("Escape");
    await page.waitForTimeout(100);
    assert.equal(await painelVisivel(page), false);

    const linha = page.locator("#escritorioTabela tbody tr").first();
    await linha.focus();
    await page.keyboard.press("Enter");
    await page.waitForTimeout(250);
    assert.equal(await painelVisivel(page), true, "Enter na linha da lista deve deixar o painel visível");

    await page.keyboard.press("Escape");
    await page.setViewportSize({ width: 800, height: 800 });
    await page.waitForTimeout(80);
    const card = page.locator("#escritorioCards article").first();
    await card.focus();
    await page.keyboard.press("Enter");
    await page.waitForTimeout(250);
    assert.equal(await painelVisivel(page), true, "Enter no card deve deixar o painel visível");

    assert.deepEqual(falhas, []);
  } finally {
    await browser.close();
    await new Promise((resolve) => server.close(resolve));
  }
});
