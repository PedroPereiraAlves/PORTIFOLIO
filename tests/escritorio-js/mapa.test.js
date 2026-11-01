const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");

const mapa = JSON.parse(
  fs.readFileSync(
    path.resolve(
      __dirname,
      "../../src/PedroHub.Web/wwwroot/lib/office/assets/map/escritorio.json"
    ),
    "utf8"
  )
);

test("mapa compacto cabe em 20×14 e as salas não usam o piso listrado", () => {
  assert.equal(mapa.width, 20);
  assert.equal(mapa.height, 14);
  assert.ok(mapa.width * mapa.height <= 10 * 8 * 6, "área total maior que 6 salas de 10×8");
  const chao = mapa.layers.find((l) => l.name === "Chao");
  assert.ok(chao);
  // GIDs 1–3 = piso-hub, piso-clinica, piso-corredor (listras azuis).
  const listras = chao.data.filter((g) => g === 1 || g === 2 || g === 3);
  assert.equal(listras.length, 0);
  const madeira = chao.data.some((g) => g === 4);
  const ladrilho = chao.data.some((g) => g === 5);
  const tapete = chao.data.some((g) => g === 24);
  assert.equal(madeira && ladrilho && tapete, true);
});

test("doze spawns e pontos de reunião e copa", () => {
  const objetos = mapa.layers.find((l) => l.name === "Objetos").objects;
  const spawns = objetos.filter((o) => o.type === "spawn").map((o) => o.properties.find((p) => p.name === "slug").value);
  assert.deepEqual(
    [...spawns].sort(),
    [
      "backend-clinica",
      "backend-hub",
      "design-clinica",
      "front-clinica",
      "front-hub",
      "gideon",
      "infra-clinica",
      "infra-testes-hub",
      "monitoramento-hub",
      "seguranca-clinica",
      "seguranca-hub",
      "testes-clinica",
    ]
  );
  assert.equal(objetos.filter((o) => o.type === "ponto-reuniao").length, 6);
  assert.ok(objetos.filter((o) => o.type === "ponto-lounge").length >= 4);
});
