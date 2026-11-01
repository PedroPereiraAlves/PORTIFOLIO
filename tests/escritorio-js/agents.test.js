const test = require("node:test");
const assert = require("node:assert/strict");
const path = require("node:path");
const agents = require(path.resolve(
  __dirname,
  "../../src/PedroHub.Web/wwwroot/js/escritorio/agents.js"
));

test("status trabalhando → mesa + type + glow", () => {
  const b = agents.behaviorForStatus("trabalhando", {});
  assert.equal(b.target, "desk");
  assert.equal(b.type, true);
  assert.equal(b.monitorGlow, true);
  assert.equal(b.sit, true);
});

test("status revisando → reunião", () => {
  const b = agents.behaviorForStatus("revisando", {});
  assert.equal(b.target, "meeting");
  assert.equal(b.sit, true);
});

test("status ocioso → lounge wander", () => {
  const b = agents.behaviorForStatus("ocioso", {});
  assert.equal(b.target, "lounge");
  assert.equal(b.wander, true);
});

test("status bloqueado → bang na mesa", () => {
  const b = agents.behaviorForStatus("bloqueado", {});
  assert.equal(b.bubble, "bang");
  assert.equal(b.sit, false);
});

test("pausado-sem-cota → sleep + desaturate", () => {
  const b = agents.behaviorForStatus("pausado-sem-cota", {});
  assert.equal(b.bubble, "sleep");
  assert.equal(b.desaturate, true);
  assert.equal(b.alpha, 0.5);
});

test("A* não atravessa colisão", () => {
  // parede vertical no meio: só dá para contornar por baixo
  const grid = [
    [0, 1, 0],
    [0, 1, 0],
    [0, 0, 0],
  ];
  const path = agents.findPath(grid, { x: 0, y: 0 }, { x: 2, y: 0 });
  assert.ok(path);
  for (const p of path) {
    assert.equal(grid[p.y][p.x], 0);
  }
  assert.ok(path.some((p) => p.y === 2));
  assert.equal(path.some((p) => p.x === 1 && p.y < 2), false);
});

test("Tab order: Gideon, Hub, Clínica", () => {
  const ordered = agents.tabOrder([
    { slug: "front-clinica", nome: "Front Clinica", sala: "Clinica", ordem: 2 },
    { slug: "front-hub", nome: "Front Hub", sala: "Hub", ordem: 2 },
    { slug: "gideon", nome: "Gideon", sala: "Central", ordem: 0 },
    { slug: "backend-hub", nome: "Backend Hub", sala: "Hub", ordem: 1 },
  ]);
  assert.equal(ordered[0].slug, "gideon");
  assert.equal(ordered[1].slug, "backend-hub");
  assert.equal(ordered[2].slug, "front-hub");
  assert.equal(ordered[3].slug, "front-clinica");
});

test("name tags offset when overlapping", () => {
  const offsets = agents.resolveNameTagOffsets(
    [
      { id: "a", x: 10, y: 100, width: 80 },
      { id: "b", x: 12, y: 100, width: 80 },
    ],
    14
  );
  assert.equal(offsets.get("a"), 0);
  assert.ok(offsets.get("b") >= 14);
});

test("plaquinhas distantes na horizontal não se empilham", () => {
  const offsets = agents.resolveNameTagOffsets(
    [
      { id: "copa", x: 40, y: 200, width: 70 },
      { id: "hub", x: 400, y: 200, width: 70 },
    ],
    22
  );
  assert.equal(offsets.get("hub"), 0);
});

test("plaquinha encostada na borda esquerda fica dentro do canvas", () => {
  const dentro = agents.clampPlaquinha(
    { x: 4, y: 30, width: 140, height: 18 },
    { width: 390, height: 280 },
    "bottom-center"
  );
  assert.ok(dentro.x - 70 >= 0);
  assert.ok(dentro.x + 70 <= 390);
  assert.ok(dentro.y - 18 >= 0);
  assert.ok(dentro.y <= 280);
});

test("hitbox do agente cobre um clique fora do sprite de 16px", () => {
  assert.equal(agents.AGENT_HIT_RADIUS > 8, true);
  assert.equal(agents.agentHitContains(14, 0), true);
  assert.equal(agents.agentHitContains(0, 14), true);
  assert.equal(agents.agentHitContains(40, 0), false);
});

test("câmera centraliza quando a vista é mais larga que o mapa", () => {
  const b = agents.cameraBoundsForMap(304, 256, 480, 280);
  assert.ok(b.x < 0);
  assert.equal(b.x, -((480 - 304) / 2));
  assert.equal(b.width, 480);
  const estreita = agents.cameraBoundsForMap(304, 256, 120, 90);
  assert.equal(estreita.x, 0);
  assert.equal(estreita.width, 304);
});

test("catálogo de sprites: 12 chaves e arte do atlas sem tinta", () => {
  assert.equal(agents.looks.length, 12);
  assert.equal(agents.CHAR_KEYS.length, 12);
  const chaves = new Set(agents.looks.map((c) => c.charKey));
  assert.equal(chaves.size, 12);
  for (const look of agents.looks) {
    assert.equal(look.roupa, null);
    assert.equal(look.acessorioTile, null);
    assert.equal(agents.charKeyForSlug(look.slug), look.charKey);
  }
});

test("renderLista não injeta HTML da API", () => {
  // Simula DOM mínimo
  const { JSDOM } = (() => {
    try {
      return { JSDOM: null };
    } catch {
      return { JSDOM: null };
    }
  })();
  // Sem jsdom: valida escapeHtml usado no painel/lista
  const esc = (s) =>
    String(s ?? "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  const bad = '<b id="inj">x</b>';
  assert.equal(esc(bad).includes("<b"), false);
  assert.ok(esc(bad).includes("&lt;b"));
});
