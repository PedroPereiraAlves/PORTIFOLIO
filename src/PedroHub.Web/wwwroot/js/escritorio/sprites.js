/**
 * Ponto único dos sprites do escritório.
 * Trocar a arte = manter `charKey` (mesmas chaves do atlas) e, se o desenho
 * novo já distinguir o elenco, zerar `roupa` e `acessorioTile`.
 */
(function (root, factory) {
  if (typeof module === "object" && module.exports) {
    module.exports = factory();
  } else {
    root.EscritorioSprites = factory();
  }
})(typeof self !== "undefined" ? self : this, function () {
  "use strict";

  const TILE_MARGIN = 1;
  const TILE_STEP = 18;

  /** @type {Array<{slug:string,charKey:string,roupa:number|null,acessorioTile:number|null}>} */
  const CAST = [
    { slug: "gideon", charKey: "gideon", roupa: 0xffd56a, acessorioTile: 27 },
    { slug: "backend-hub", charKey: "backend-hub", roupa: 0x3b82f6, acessorioTile: 17 },
    { slug: "front-hub", charKey: "front-hub", roupa: 0xec4899, acessorioTile: 24 },
    { slug: "monitoramento-hub", charKey: "monitoramento-hub", roupa: 0x22c55e, acessorioTile: 18 },
    { slug: "seguranca-hub", charKey: "seguranca-hub", roupa: 0xef4444, acessorioTile: 26 },
    { slug: "infra-testes-hub", charKey: "infra-testes-hub", roupa: 0xf97316, acessorioTile: 29 },
    { slug: "backend-clinica", charKey: "backend-clinica", roupa: 0x8b5cf6, acessorioTile: 28 },
    { slug: "front-clinica", charKey: "front-clinica", roupa: 0x06b6d4, acessorioTile: 25 },
    { slug: "seguranca-clinica", charKey: "seguranca-clinica", roupa: 0xeab308, acessorioTile: 13 },
    { slug: "infra-clinica", charKey: "infra-clinica", roupa: 0x14b8a6, acessorioTile: 15 },
    { slug: "design-clinica", charKey: "design-clinica", roupa: 0xd946ef, acessorioTile: 23 },
    { slug: "testes-clinica", charKey: "testes-clinica", roupa: 0x84cc16, acessorioTile: 16 },
  ];

  const bySlug = new Map(CAST.map((c) => [c.slug, c]));
  const CHAR_KEYS = CAST.map((c) => c.charKey);

  function lookFor(slug, index) {
    if (bySlug.has(slug)) return bySlug.get(slug);
    return CAST[(index || 0) % CAST.length];
  }

  function charKeyForSlug(slug, index) {
    return lookFor(slug, index).charKey;
  }

  /** Origem do tile no atlas office-16 (margin 1, spacing 2). */
  function tileOrigin(tileIndex) {
    const col = tileIndex % 16;
    const row = Math.floor(tileIndex / 16);
    return {
      sx: TILE_MARGIN + col * TILE_STEP,
      sy: TILE_MARGIN + row * TILE_STEP,
    };
  }

  return {
    CAST,
    CHAR_KEYS,
    lookFor,
    charKeyForSlug,
    tileOrigin,
  };
});
