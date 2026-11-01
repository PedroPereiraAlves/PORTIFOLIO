/**
 * Ponto único dos sprites do escritório.
 * `charKey` é a chave no atlas. `roupa` e `acessorioTile` ficam null enquanto
 * a arte do atlas já distinguir o elenco; preencha só se um boneco novo
 * precisar de tinta ou de um tile por cima.
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
    { slug: "gideon", charKey: "gideon", roupa: null, acessorioTile: null },
    { slug: "backend-hub", charKey: "backend-hub", roupa: null, acessorioTile: null },
    { slug: "front-hub", charKey: "front-hub", roupa: null, acessorioTile: null },
    { slug: "monitoramento-hub", charKey: "monitoramento-hub", roupa: null, acessorioTile: null },
    { slug: "seguranca-hub", charKey: "seguranca-hub", roupa: null, acessorioTile: null },
    { slug: "infra-testes-hub", charKey: "infra-testes-hub", roupa: null, acessorioTile: null },
    { slug: "backend-clinica", charKey: "backend-clinica", roupa: null, acessorioTile: null },
    { slug: "front-clinica", charKey: "front-clinica", roupa: null, acessorioTile: null },
    { slug: "seguranca-clinica", charKey: "seguranca-clinica", roupa: null, acessorioTile: null },
    { slug: "infra-clinica", charKey: "infra-clinica", roupa: null, acessorioTile: null },
    { slug: "design-clinica", charKey: "design-clinica", roupa: null, acessorioTile: null },
    { slug: "testes-clinica", charKey: "testes-clinica", roupa: null, acessorioTile: null },
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
