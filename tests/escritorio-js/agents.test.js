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
      { id: "a", x: 10, y: 100 },
      { id: "b", x: 12, y: 100 },
    ],
    14
  );
  assert.equal(offsets.get("a"), 0);
  assert.ok(offsets.get("b") >= 14);
});
