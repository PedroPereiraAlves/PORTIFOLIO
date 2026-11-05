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

  function catalogoSprites() {
    if (typeof module === "object" && module.exports) {
      return require("./sprites.js");
    }
    const host = typeof self !== "undefined" ? self : globalThis;
    if (!host.EscritorioSprites) {
      throw new Error("Carregue sprites.js antes de agents.js");
    }
    return host.EscritorioSprites;
  }

  const sprites = catalogoSprites();

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

    let behavior;
    switch (status) {
      case STATUS.trabalhando:
        behavior = {
          ...base,
          target: "desk",
          sit: true,
          type: true,
          anim: "type",
          monitorGlow: true,
        };
        break;
      case STATUS.revisando:
        behavior = {
          ...base,
          target: "meeting",
          sit: true,
          anim: "sit-down",
        };
        break;
      case STATUS.ocioso:
        behavior = {
          ...base,
          target: "lounge",
          wander: true,
          anim: "walk-down",
        };
        break;
      case STATUS.bloqueado:
        behavior = {
          ...base,
          target: "desk",
          sit: false,
          anim: "idle-down",
          bubble: "bang",
        };
        break;
      case STATUS["pausado-sem-cota"]:
        behavior = {
          ...base,
          target: "desk",
          sit: true,
          alpha: 0.5,
          desaturate: true,
          bubble: "sleep",
          anim: "sit-down",
        };
        break;
      default:
        behavior = { ...base, target: "desk", anim: "idle-down" };
        break;
    }

    // Desatualizado (EscritorioOptions.MinutosDesatualizado) só avisa:
    // bolha de relógio e alpha leve. Wander/walk do status continuam.
    if (desatualizado) {
      return {
        ...behavior,
        desatualizado: true,
        alpha: 0.6,
        bubble: "clock",
      };
    }

    return behavior;
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

  /** Empurra plaquinhas que se sobrepõem na horizontal. Quem está longe em X não se desloca. */
  function resolveNameTagOffsets(tags, minGap) {
    const gap = minGap || 20;
    const sorted = [...tags].sort((a, b) => a.y - b.y || a.x - b.x);
    const placed = [];
    const out = new Map();
    for (const t of sorted) {
      const half = (t.width || 88) / 2;
      let y = t.y;
      for (let guard = 0; guard < 16; guard++) {
        const hit = placed.find((p) => {
          const xOverlap = Math.abs(p.x - t.x) < p.half + half - 4;
          return xOverlap && y < p.y + gap && p.y < y + gap;
        });
        if (!hit) break;
        y = hit.y + gap;
      }
      placed.push({ x: t.x, y, half });
      out.set(t.id, y - t.y);
    }
    return out;
  }

  /**
   * Mantém a plaquinha dentro do canvas.
   * anchor "bottom-center": left/top é o centro da base (translate -50% -100%).
   * anchor "top-center": left/top é o centro do topo.
   */
  function clampPlaquinha(tag, canvas, anchor) {
    const margin = 4;
    const half = Math.max(0, tag.width / 2);
    const minX = half + margin;
    const maxX = canvas.width - half - margin;
    const x = maxX < minX ? canvas.width / 2 : Math.min(maxX, Math.max(minX, tag.x));
    let y = tag.y;
    if (anchor === "top-center") {
      const minY = margin;
      const maxY = canvas.height - tag.height - margin;
      y = maxY < minY ? margin : Math.min(maxY, Math.max(minY, tag.y));
    } else {
      const minY = tag.height + margin;
      const maxY = canvas.height - margin;
      y = maxY < minY ? canvas.height / 2 : Math.min(maxY, Math.max(minY, tag.y));
    }
    return { x, y };
  }

  /** Raio em pixels do mundo. Maior que o sprite 16×16 para o clique pegar quem está andando. */
  const AGENT_HIT_RADIUS = 16;

  function agentHitContains(dx, dy) {
    return dx * dx + dy * dy <= AGENT_HIT_RADIUS * AGENT_HIT_RADIUS;
  }

  /** Bounds da câmera para o mapa ficar centrado quando a vista é maior que o mundo. */
  function cameraBoundsForMap(mapW, mapH, viewW, viewH) {
    const padX = Math.max(0, (viewW - mapW) / 2);
    const padY = Math.max(0, (viewH - mapH) / 2);
    return {
      x: padX === 0 ? 0 : -padX,
      y: padY === 0 ? 0 : -padY,
      width: mapW + padX * 2,
      height: mapH + padY * 2,
    };
  }

  const CHAR_KEYS = sprites.CHAR_KEYS;

  function charKeyForSlug(slug, index) {
    return sprites.charKeyForSlug(slug, index);
  }

  return {
    STATUS,
    behaviorForStatus,
    tabOrder,
    findPath,
    resolveNameTagOffsets,
    clampPlaquinha,
    AGENT_HIT_RADIUS,
    agentHitContains,
    cameraBoundsForMap,
    CHAR_KEYS,
    charKeyForSlug,
    looks: sprites.CAST,
  };
});
