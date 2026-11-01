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

    await page.setViewportSize({ width: 1280, height: 800 });
    await page.waitForTimeout(500);

    const toggle = page.locator("#escritorioToggleMapLista");
    assert.equal((await toggle.locator("[data-mode-label]").textContent())?.trim(), "Lista");
    assert.equal(await toggle.getAttribute("aria-label"), "Ver lista");
    assert.equal(await toggle.getAttribute("aria-pressed"), "true");
    assert.equal(await page.evaluate(() => window.__escritorio), undefined);

    const fora = await page.evaluate(() => {
      const canvas = document.querySelector("#escritorio-game").getBoundingClientRect();
      const bad = [];
      for (const el of document.querySelectorAll(".escritorio-nametag, .escritorio-room-label")) {
        if (el.style.display === "none") continue;
        const r = el.getBoundingClientRect();
        if (r.width < 2 || r.height < 2) continue;
        if (
          r.left < canvas.left - 1 ||
          r.right > canvas.right + 1 ||
          r.top < canvas.top - 1 ||
          r.bottom > canvas.bottom + 1
        ) {
          bad.push(el.textContent.trim());
        }
      }
      return bad;
    });
    assert.deepEqual(fora, [], "plaquinha fora do canvas: " + fora.join(", "));

    await page.keyboard.press("Escape");
    await page.waitForTimeout(150);
    const nome = "Front Clinica";
    const pe = () =>
      page.evaluate((n) => {
        const canvas = document.querySelector("#escritorio-game").getBoundingClientRect();
        const el = [...document.querySelectorAll(".escritorio-nametag")].find(
          (tag) => tag.textContent.includes(n) && tag.dataset.footX
        );
        if (!el || el.style.display === "none") return null;
        return {
          x: canvas.left + Number(el.dataset.footX),
          y: canvas.top + Number(el.dataset.footY) - 10,
          inside:
            Number(el.dataset.footX) > 8 &&
            Number(el.dataset.footX) < canvas.width - 8 &&
            Number(el.dataset.footY) > 8 &&
            Number(el.dataset.footY) < canvas.height - 8,
        };
      }, nome);
    const inicio = await pe();
    assert.ok(inicio, "posição de " + nome);
    let abriu = false;
    for (let i = 0; i < 28 && !abriu; i++) {
      await page.waitForTimeout(250);
      const agora = await pe();
      if (!agora?.inside) continue;
      if (Math.hypot(agora.x - inicio.x, agora.y - inicio.y) < 12) continue;
      await page.mouse.click(agora.x, agora.y);
      await page.waitForTimeout(200);
      const titulo = (await page.locator("#escritorioPanelTitle").textContent()) || "";
      if ((await painelVisivel(page)) && titulo.includes("Front Clinica")) {
        abriu = true;
        break;
      }
      await page.keyboard.press("Escape");
      await page.waitForTimeout(120);
    }
    assert.equal(abriu, true, "clique no agente andando deve abrir o painel dele");

    assert.deepEqual(falhas, []);
  } finally {
    await browser.close();
    await new Promise((resolve) => server.close(resolve));
  }
});

test("no celular o toggle alterna Mapa e Lista", async () => {
  const { server, url } = await startHarness();
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
  try {
    await page.goto(url, { waitUntil: "domcontentloaded" });
    await page.waitForFunction(() => {
      const b = document.querySelector("#escritorioToggleMapLista [data-mode-label]");
      return b && b.textContent.trim() === "Mapa";
    });
    const toggle = page.locator("#escritorioToggleMapLista");
    assert.equal(await toggle.getAttribute("aria-label"), "Ver mapa");
    assert.equal(await toggle.getAttribute("aria-pressed"), "false");
    const mapaOculto = await page.locator("#escritorioMapPane").evaluate((el) => el.hasAttribute("hidden"));
    assert.equal(mapaOculto, true);

    await toggle.click();
    await page.waitForFunction(() => {
      const b = document.querySelector("#escritorioToggleMapLista [data-mode-label]");
      return b && b.textContent.trim() === "Lista";
    });
    assert.equal(await toggle.getAttribute("aria-label"), "Ver lista");
    assert.equal(await toggle.getAttribute("aria-pressed"), "true");
    const mapaVisivel = await page.locator("#escritorioMapPane").evaluate((el) => !el.hasAttribute("hidden"));
    assert.equal(mapaVisivel, true);
  } finally {
    await browser.close();
    await new Promise((resolve) => server.close(resolve));
  }
});
