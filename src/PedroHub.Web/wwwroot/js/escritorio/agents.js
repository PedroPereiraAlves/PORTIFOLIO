/**
 * Mapeamento status → comportamento (sem regra de negócio; só tradução visual).
 * Exportável para testes Node.
 */
(function (root, factory) {
  if (typeof module === "object" && module.exports) {
    module.exports = factory();
  } else {
    root.EscritorioAgents = factory();
  }
})(typeof self !== "undefined" ? self : this, function () {
  "use strict";

  const STATUS = {
    trabalhando: "trabalhando",
    revisando: "revisando",
    ocioso: "ocioso",
    bloqueado: "bloqueado",
    "pausado-sem-cota": "pausado-sem-cota",
  };

  /** @param {string} status @param {{desatualizado?: boolean}} opts */
  function behaviorForStatus(status, opts) {
    const desatualizado = !!(opts && opts.desatualizado);
    const base = {
      status,
      desatualizado,
      target: "desk", // desk | meeting | lounge
      anim: "idle-down",
      sit: false,
      type: false,
      alpha: 1,
      desaturate: false,
      bubble: null, // bang | sleep | clock | null
      monitorGlow: false,
      wander: false,
    };

    if (desatualizado) {
      return { ...base, alpha: 0.6, bubble: "clock", target: "desk", sit: true };
    }

    switch (status) {
      case STATUS.trabalhando:
        return {
          ...base,
          target: "desk",
          sit: true,
          type: true,
          anim: "type",
          monitorGlow: true,
        };
      case STATUS.revisando:
        return {
          ...base,
          target: "meeting",
          sit: true,
          anim: "sit-down",
        };
      case STATUS.ocioso:
        return {
          ...base,
          target: "lounge",
          wander: true,
          anim: "walk-down",
        };
      case STATUS.bloqueado:
        return {
          ...base,
          target: "desk",
          sit: false,
          anim: "idle-down",
          bubble: "bang",
        };
      case STATUS["pausado-sem-cota"]:
        return {
          ...base,
          target: "desk",
          sit: true,
          alpha: 0.5,
          desaturate: true,
          bubble: "sleep",
          anim: "sit-down",
        };
      default:
        return { ...base, target: "desk", anim: "idle-down" };
    }
  }

  /** Ordem Tab: Gideon, Hub, Clínica */
  function tabOrder(agentes) {
    const salaRank = (s) => {
      const v = (s || "").toLowerCase();
      if (v === "central" || v === "gideon") return 0;
      if (v === "hub") return 1;
      if (v === "clinica" || v === "clínica") return 2;
      return 3;
    };
    return [...agentes].sort((a, b) => {
      if ((a.slug || "") === "gideon") return -1;
      if ((b.slug || "") === "gideon") return 1;
      const sr = salaRank(a.sala) - salaRank(b.sala);
      if (sr !== 0) return sr;
      return (a.ordem ?? 0) - (b.ordem ?? 0) || String(a.nome).localeCompare(String(b.nome));
    });
  }

  /**
   * A* grid (4-dir). grid[y][x] === 0 walkable, 1 blocked.
   * @returns {Array<{x:number,y:number}>|null}
   */
  function findPath(grid, start, end) {
    const h = grid.length;
    const w = grid[0].length;
    const key = (x, y) => x + "," + y;
    const inBounds = (x, y) => x >= 0 && y >= 0 && x < w && y < h;
    if (!inBounds(start.x, start.y) || !inBounds(end.x, end.y)) return null;
    if (grid[end.y][end.x] === 1) return null;

    const open = [{ x: start.x, y: start.y, g: 0, f: 0, parent: null }];
    const closed = new Set();
    const heur = (x, y) => Math.abs(x - end.x) + Math.abs(y - end.y);

    while (open.length) {
      open.sort((a, b) => a.f - b.f);
      const cur = open.shift();
      const ck = key(cur.x, cur.y);
      if (closed.has(ck)) continue;
      closed.add(ck);
      if (cur.x === end.x && cur.y === end.y) {
        const path = [];
        let n = cur;
        while (n) {
          path.push({ x: n.x, y: n.y });
          n = n.parent;
        }
        return path.reverse();
      }
      for (const [dx, dy] of [
        [0, -1],
        [0, 1],
        [-1, 0],
        [1, 0],
      ]) {
        const nx = cur.x + dx;
        const ny = cur.y + dy;
        if (!inBounds(nx, ny) || grid[ny][nx] === 1) continue;
        const nk = key(nx, ny);
        if (closed.has(nk)) continue;
        const g = cur.g + 1;
        open.push({ x: nx, y: ny, g, f: g + heur(nx, ny), parent: cur });
      }
    }
    return null;
  }

  /** Resolve name-tag offsets to avoid overlap (by y pixel). */
  function resolveNameTagOffsets(tags, minGap) {
    const gap = minGap || 14;
    const sorted = [...tags].sort((a, b) => a.y - b.y || a.x - b.x);
    const out = new Map();
    let lastY = -Infinity;
    for (const t of sorted) {
      let y = t.y;
      if (y < lastY + gap) y = lastY + gap;
      out.set(t.id, y - t.y);
      lastY = y;
    }
    return out;
  }

  const CHAR_KEYS = [
    "samurai_blue",
    "samurai_green",
    "samurai_blue-v1",
    "samurai_blue-v2",
    "samurai_blue-v3",
    "samurai_blue-v4",
    "samurai_blue-v5",
    "samurai_blue-v6",
    "samurai_blue-v7",
    "samurai_blue-v8",
    "samurai_blue-v9",
    "samurai_blue-v10",
  ];

  function charKeyForSlug(slug, index) {
    if (slug === "gideon") return CHAR_KEYS[0];
    return CHAR_KEYS[(index % (CHAR_KEYS.length - 1)) + 1];
  }

  return {
    STATUS,
    behaviorForStatus,
    tabOrder,
    findPath,
    resolveNameTagOffsets,
    CHAR_KEYS,
    charKeyForSlug,
  };
});
