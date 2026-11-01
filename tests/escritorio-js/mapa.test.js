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

test("mapa usa piso padrão, borda na parede e carpete", () => {
  assert.equal(mapa.width, 19);
  assert.equal(mapa.height, 16);
  const chao = mapa.layers.find((l) => l.name === "Chao");
  assert.ok(chao);
  // gid = índice do atlas + 1. 1 = tile 0, 3 = tile 2, 4 e 5 = tiles 3–4.
  assert.ok(chao.data.includes(1), "falta o piso padrão (tile 0)");
  assert.ok(chao.data.includes(3), "falta a madeira com borda (tile 2)");
  assert.ok(chao.data.includes(4), "falta o carpete da reunião (tile 3)");
  assert.ok(chao.data.includes(5), "falta o carpete da copa (tile 4)");
});

test("atlas traz sleep_0 e sleep_1 para os 12 personagens", () => {
  const atlas = JSON.parse(
    fs.readFileSync(
      path.resolve(
        __dirname,
        "../../src/PedroHub.Web/wwwroot/lib/office/assets/chars/agents.json"
      ),
      "utf8"
    )
  );
  const slugs = [
    "gideon",
    "backend-hub",
    "front-hub",
    "monitoramento-hub",
    "seguranca-hub",
    "infra-testes-hub",
    "backend-clinica",
    "front-clinica",
    "seguranca-clinica",
    "infra-clinica",
    "design-clinica",
    "testes-clinica",
  ];
  for (const slug of slugs) {
    assert.ok(atlas.frames[slug + "_sleep_0"], slug + "_sleep_0");
    assert.ok(atlas.frames[slug + "_sleep_1"], slug + "_sleep_1");
  }
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
