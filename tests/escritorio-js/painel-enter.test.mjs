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

async function prepararCsp(page) {
  const consoleCsp = [];
  await page.addInitScript(() => {
    window.__cspViolations = [];
    document.addEventListener("securitypolicyviolation", (e) => {
      window.__cspViolations.push((e.violatedDirective || "") + " " + (e.blockedURI || ""));
    });
  });
  page.on("console", (msg) => {
    if (/Content Security Policy|Refused to execute inline script|Refused to load the script/i.test(msg.text())) {
      consoleCsp.push(msg.text());
    }
  });
  return consoleCsp;
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
    await page.waitForSelector("#escritorio-game canvas", { timeout: 15000 });
    await page.waitForFunction(() =>
      [...document.querySelectorAll(".escritorio-nametag")].some(
        (el) => el.style.display !== "none" && el.getBoundingClientRect().width > 2
      )
    );
    const fora = await page.evaluate(() => {
      const canvas = document.querySelector("#escritorio-game").getBoundingClientRect();
      const visiveis = [];
      const bad = [];
      for (const el of document.querySelectorAll(".escritorio-nametag")) {
        if (el.style.display === "none") continue;
        const r = el.getBoundingClientRect();
        if (r.width < 2) continue;
        visiveis.push(el.textContent.trim());
        if (
          r.left < canvas.left - 1 ||
          r.right > canvas.right + 1 ||
          r.top < canvas.top - 1 ||
          r.bottom > canvas.bottom + 1
        ) {
          bad.push(el.textContent.trim());
        }
      }
      return { visiveis, bad };
    });
    assert.ok(fora.visiveis.length > 0, "nenhuma plaquinha de agente na área visível");
    assert.deepEqual(fora.bad, []);
  } finally {
    await browser.close();
    await new Promise((resolve) => server.close(resolve));
  }
});

test("data-debug liga __escritorio sem violar a CSP", async () => {
  const { server, url } = await startHarness();
  const browser = await chromium.launch({ headless: true });
  try {
    const sem = await browser.newPage();
    const cspSem = await prepararCsp(sem);
    await sem.goto(url, { waitUntil: "networkidle" });
    await sem.waitForSelector("#escritorio-game");
    assert.equal(await sem.locator("#escritorio-game").getAttribute("data-debug"), null);
    assert.equal(await sem.evaluate(() => window.__escritorio), undefined);
    assert.deepEqual(await sem.evaluate(() => window.__cspViolations), []);
    assert.deepEqual(cspSem, []);

    const com = await browser.newPage();
    const cspCom = await prepararCsp(com);
    await com.goto(url + "?debug=1", { waitUntil: "networkidle" });
    await com.waitForFunction(() => window.__escritorio && typeof window.__escritorio.isPanelOpen === "function");
    assert.equal(await com.locator("#escritorio-game").getAttribute("data-debug"), "true");
    assert.deepEqual(await com.evaluate(() => window.__cspViolations), []);
    assert.deepEqual(cspCom, []);
  } finally {
    await browser.close();
    await new Promise((resolve) => server.close(resolve));
  }
});

test("clique no Front Hub enquanto a posição muda abre o painel dele", async () => {
  const { server, url } = await startHarness();
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  try {
    await page.goto(url + "?walk=front-hub", { waitUntil: "networkidle" });
    await page.waitForSelector("#escritorio-game canvas", { timeout: 15000 });
    const pe = () =>
      page.evaluate(() => {
        const canvas = document.querySelector("#escritorio-game").getBoundingClientRect();
        const el = [...document.querySelectorAll(".escritorio-nametag")].find(
          (tag) => tag.textContent.includes("Front Hub")
        );
        if (!el || el.style.display === "none" || !el.dataset.footX) return null;
        const footX = Number(el.dataset.footX);
        const footY = Number(el.dataset.footY);
        return {
          x: canvas.left + footX,
          y: canvas.top + footY - 8,
          inside: footX > 12 && footX < canvas.width - 12 && footY > 12 && footY < canvas.height - 12,
        };
      });
    let anterior = null;
    let emMovimento = null;
    for (let i = 0; i < 40 && !emMovimento; i++) {
      const agora = await pe();
      if (anterior && agora?.inside && Math.hypot(agora.x - anterior.x, agora.y - anterior.y) > 3) {
        emMovimento = { anterior, agora };
        break;
      }
      anterior = agora || anterior;
      await page.waitForTimeout(80);
    }
    assert.ok(emMovimento, "Front Hub não estava em movimento dentro do canvas");
    assert.ok(
      Math.hypot(emMovimento.agora.x - emMovimento.anterior.x, emMovimento.agora.y - emMovimento.anterior.y) > 3
    );
    await page.mouse.click(emMovimento.agora.x, emMovimento.agora.y);
    await page.waitForTimeout(250);
    assert.equal(await painelVisivel(page), true);
    assert.match((await page.locator("#escritorioPanelTitle").textContent()) || "", /Front Hub/);
  } finally {
    await browser.close();
    await new Promise((resolve) => server.close(resolve));
  }
});
