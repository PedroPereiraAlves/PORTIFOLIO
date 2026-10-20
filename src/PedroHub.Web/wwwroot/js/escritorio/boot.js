(function () {
  "use strict";

  const ESTADO_URL = "/admin/escritorio/estado";
  const MANIFEST_URL = "/lib/office/assets/manifest.json";
  const SCRIPT_SRCS = [
    "/lib/office/phaser/phaser.min.js",
    "/lib/office/easystar/easystar.min.js",
    "/js/escritorio/agents.js",
    "/js/escritorio/ui-panel.js",
    "/js/escritorio/scene-office.js",
  ];

  const mapPane = document.getElementById("escritorioMapPane");
  const gameHost = document.getElementById("escritorio-game");
  const layout = document.querySelector(".escritorio-layout");
  const toggleMapLista = document.getElementById("escritorioToggleMapLista");
  const btnCenter = document.getElementById("escritorioCenter");
  const loadWarn = document.getElementById("escritorioLoadWarn");
  const liveSel = document.getElementById("escritorioSelecaoLive");

  let game = null;
  let scene = null;
  let panel = null;
  let pollingSegundos = 20;
  let timer = null;
  let showingMap = window.innerWidth >= 720;
  let phaserLoaded = false;
  let phaserStarted = false;
  let cachedManifest = null;
  let cachedEstado = null;
  let lastFocusEl = null;

  const Agents = window.EscritorioAgents;

  function escapeHtml(s) {
    return String(s ?? "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function normalize(a) {
    return {
      slug: a.slug ?? a.Slug,
      nome: a.nome ?? a.Nome,
      papel: a.papel ?? a.Papel,
      sala: a.sala ?? a.Sala,
      ordem: a.ordem ?? a.Ordem,
      status: a.status ?? a.Status,
      tarefaAtual: a.tarefaAtual ?? a.TarefaAtual,
      projeto: a.projeto ?? a.Projeto,
      prUrl: a.prUrl ?? a.PrUrl,
      haQuantoTempo: a.haQuantoTempo ?? a.HaQuantoTempo,
      desatualizado: a.desatualizado ?? a.Desatualizado,
      ultimasEntregas: a.ultimasEntregas ?? a.UltimasEntregas ?? [],
      corAvatar: a.corAvatar ?? a.CorAvatar,
    };
  }

  function setupPanel() {
    panel = window.EscritorioUiPanel.createPanelController({
      panel: document.getElementById("escritorioPanel"),
      title: document.getElementById("escritorioPanelTitle"),
      papel: document.getElementById("escritorioPanelPapel"),
      status: document.getElementById("escritorioPanelStatus"),
      projeto: document.getElementById("escritorioPanelProjeto"),
      quando: document.getElementById("escritorioPanelQuando"),
      tarefa: document.getElementById("escritorioPanelTarefa"),
      pr: document.getElementById("escritorioPanelPr"),
      entregas: document.getElementById("escritorioPanelEntregas"),
      closeBtn: document.getElementById("escritorioPanelClose"),
      portrait: document.getElementById("escritorioPanelPortrait"),
      onClose: function () {
        layout?.classList.remove("is-panel-open");
        gameHost?.focus();
      },
      onOpen: function () {
        layout?.classList.add("is-panel-open");
      },
    });
  }

  function loadScript(src) {
    return new Promise(function (resolve, reject) {
      if (document.querySelector('script[src="' + src + '"]')) {
        resolve();
        return;
      }
      const s = document.createElement("script");
      s.src = src;
      s.defer = true;
      s.onload = function () {
        resolve();
      };
      s.onerror = function () {
        reject(new Error(src));
      };
      document.body.appendChild(s);
    });
  }

  async function ensurePhaserScripts() {
    if (phaserLoaded) return;
    for (const src of SCRIPT_SRCS) {
      await loadScript(src);
    }
    // agents/ui/scene attach to window; panel needs recreate if ui just loaded
    if (!panel && window.EscritorioUiPanel) setupPanel();
    phaserLoaded = true;
  }

  async function loadManifest() {
    const res = await fetch(MANIFEST_URL, { credentials: "same-origin" });
    if (!res.ok) throw new Error("manifest");
    return res.json();
  }

  async function fetchEstado() {
    const res = await fetch(ESTADO_URL, {
      headers: { Accept: "application/json" },
      credentials: "same-origin",
    });
    if (!res.ok) throw new Error("estado " + res.status);
    const data = await res.json();
    return {
      pollingSegundos: data.pollingSegundos || data.PollingSegundos || 20,
      agentes: (data.agentes || data.Agentes || []).map(normalize),
    };
  }

  function setPortraitFromAtlas(charKey) {
    const canvas = document.getElementById("escritorioPanelPortrait");
    if (!canvas || !charKey || !game) return;
    const tex = game.textures.get("agents");
    if (!tex || !tex.has(charKey + "_face")) {
      canvas.hidden = true;
      return;
    }
    const frame = tex.get(charKey + "_face");
    const ctx = canvas.getContext("2d");
    ctx.imageSmoothingEnabled = false;
    ctx.clearRect(0, 0, 64, 64);
    const src = frame.source[0] || frame.texture.source[0];
    const img = src.image;
    ctx.drawImage(
      img,
      frame.cutX,
      frame.cutY,
      frame.cutWidth,
      frame.cutHeight,
      0,
      0,
      64,
      64
    );
    canvas.hidden = false;
  }

  function startPhaser(manifest, estado) {
    if (!gameHost || typeof Phaser === "undefined" || phaserStarted) return;
    phaserStarted = true;

    const OfficeScene = window.EscritorioOfficeScene;
    game = new Phaser.Game({
      type: Phaser.AUTO,
      parent: gameHost,
      width: gameHost.clientWidth || 800,
      height: gameHost.clientHeight || 480,
      backgroundColor: "#1a2433",
      pixelArt: true,
      antialias: false,
      roundPixels: true,
      loader: { imageLoadType: "HTMLImageElement" },
      scale: {
        mode: Phaser.Scale.RESIZE,
        autoCenter: Phaser.Scale.CENTER_BOTH,
      },
      scene: [],
    });
    game.scene.add("OfficeScene", OfficeScene, true, {
      manifest,
      estado,
      onHighlight: function (agente) {
        if (liveSel && agente) {
          liveSel.textContent =
            agente.nome + ", " + String(agente.status || "").replace(/-/g, " ");
        }
      },
      onSelect: function (agente, entry) {
        lastFocusEl = document.activeElement;
        panel.open(agente, { faceKey: entry?.charKey });
        setPortraitFromAtlas(entry?.charKey);
      },
      onReady: function (s) {
        scene = s;
        if (gameHost?.getAttribute("data-load-error")) {
          showLoadError();
        }
      },
      onLoadError: function () {
        showLoadError();
      },
    });
  }

  function showLoadError() {
    if (loadWarn) loadWarn.hidden = false;
    showingMap = false;
    applyViewMode();
  }

  function applyViewMode() {
    const mobile = window.innerWidth < 720;
    if (toggleMapLista) {
      toggleMapLista.setAttribute("aria-pressed", showingMap ? "true" : "false");
      toggleMapLista.textContent = showingMap ? "Mapa | Lista" : "Mapa | Lista";
      const label = toggleMapLista.querySelector("[data-mode-label]");
      if (label) label.textContent = showingMap ? "Lista" : "Mapa";
      else toggleMapLista.textContent = showingMap ? "Ver lista" : "Ver mapa";
    }
    if (mapPane) {
      if (mobile && !showingMap) mapPane.setAttribute("hidden", "hidden");
      else mapPane.removeAttribute("hidden");
    }
    const lista = document.getElementById("escritorioLista");
    if (lista) {
      if (mobile && showingMap) lista.setAttribute("hidden", "hidden");
      else lista.removeAttribute("hidden");
    }
  }

  async function showMapFirstTime() {
    showingMap = true;
    applyViewMode();
    if (!phaserStarted) {
      await ensurePhaserScripts();
      const manifest = cachedManifest || (await loadManifest());
      const estado = cachedEstado || (await fetchEstado());
      startPhaser(manifest, estado);
    }
  }

  function wireA11y() {
    if (gameHost) {
      gameHost.tabIndex = 0;
      gameHost.setAttribute("role", "application");
      gameHost.setAttribute("aria-describedby", "escritorioA11yHelp");
      gameHost.addEventListener("keydown", function (ev) {
        if (!scene) return;
        if (ev.key === "Tab") {
          const atEnd =
            !ev.shiftKey &&
            scene.tabList &&
            scene.selectedSlug === scene.tabList[scene.tabList.length - 1];
          const atStart =
            ev.shiftKey &&
            scene.tabList &&
            scene.selectedSlug === scene.tabList[0];
          if (atEnd || atStart) {
            // deixa o Tab sair do mapa
            return;
          }
          ev.preventDefault();
          scene.highlightByTab(ev.shiftKey ? -1 : 1);
        } else if (ev.key === "Enter") {
          if (scene.selectedSlug) scene.openSelected();
          else if (scene.tabList?.[0]) {
            scene.highlightSlug(scene.tabList[0]);
            scene.openSelected();
          }
        } else if (ev.key === "Escape") {
          panel?.close();
          gameHost.focus();
        } else if (ev.key === "+" || ev.key === "=") {
          scene.bumpZoom(1);
        } else if (ev.key === "-") {
          scene.bumpZoom(-1);
        } else if (ev.key === "ArrowLeft") {
          scene.cameras.main.scrollX -= 24;
        } else if (ev.key === "ArrowRight") {
          scene.cameras.main.scrollX += 24;
        } else if (ev.key === "ArrowUp") {
          scene.cameras.main.scrollY -= 24;
        } else if (ev.key === "ArrowDown") {
          scene.cameras.main.scrollY += 24;
        }
      });
    }

    toggleMapLista?.addEventListener("click", async function () {
      if (showingMap) {
        showingMap = false;
        applyViewMode();
      } else {
        await showMapFirstTime();
      }
    });
    btnCenter?.addEventListener("click", function () {
      scene?.centerMap();
    });
    document.addEventListener("keydown", function (ev) {
      if (ev.key === "Escape" && panel?.isOpen()) {
        panel.close();
        gameHost?.focus();
      }
    });
  }

  const listaLive = document.getElementById("escritorioListaLive");
  const tbody = document.querySelector("#escritorioTabela tbody");
  const cards = document.getElementById("escritorioCards");
  let statusAnterior = new Map();

  function anunciarDiffs(agentes) {
    if (!listaLive) return;
    const msgs = [];
    for (const a of agentes) {
      const prev = statusAnterior.get(a.slug);
      if (prev && prev !== a.status) {
        msgs.push(
          a.nome +
            ": " +
            prev.replace(/-/g, " ") +
            " → " +
            a.status.replace(/-/g, " ")
        );
      }
    }
    listaLive.textContent = msgs.length ? msgs.join(". ") : "";
  }

  function renderLista(agentes) {
    if (!tbody || !cards) return;
    const ordered = Agents ? Agents.tabOrder(agentes) : agentes;
    while (tbody.firstChild) tbody.removeChild(tbody.firstChild);
    while (cards.firstChild) cards.removeChild(cards.firstChild);

    for (const a of ordered) {
      const tr = document.createElement("tr");
      tr.tabIndex = 0;
      tr.dataset.slug = a.slug;
      const cells = [
        a.nome,
        a.sala,
        null,
        a.tarefaAtual || "—",
        a.haQuantoTempo || "—",
      ];
      cells.forEach(function (val, i) {
        const td = document.createElement("td");
        if (i === 2) {
          const chip = document.createElement("span");
          chip.className = "escritorio-status-chip status-" + (a.status || "");
          chip.textContent = String(a.status || "").replace(/-/g, " ");
          td.appendChild(chip);
        } else {
          td.textContent = val;
        }
        tr.appendChild(td);
      });
      tr.addEventListener("click", function () {
        scene?.highlightSlug(a.slug);
        scene?.openSelected();
        if (!scene) panel.open(a, {});
      });
      tr.addEventListener("keydown", function (ev) {
        if (ev.key === "Enter") {
          scene?.highlightSlug(a.slug);
          scene?.openSelected();
          if (!scene) panel.open(a, {});
        }
      });
      tbody.appendChild(tr);

      const card = document.createElement("article");
      card.className = "admin-card";
      card.tabIndex = 0;
      const h3 = document.createElement("h3");
      h3.className = "admin-card-title";
      h3.textContent = a.nome;
      const p = document.createElement("p");
      p.className = "muted";
      p.textContent = a.sala + " · " + String(a.status || "").replace(/-/g, " ");
      card.appendChild(h3);
      card.appendChild(p);
      card.addEventListener("click", function () {
        scene?.highlightSlug(a.slug);
        scene?.openSelected();
        if (!scene) panel.open(a, {});
      });
      cards.appendChild(card);
    }
  }

  // export for tests
  window.__escritorioRenderLista = renderLista;
  window.__escritorioEscapeHtml = escapeHtml;

  async function poll() {
    if (document.visibilityState === "hidden") return;
    try {
      const estado = await fetchEstado();
      cachedEstado = estado;
      pollingSegundos = estado.pollingSegundos;
      anunciarDiffs(estado.agentes);
      statusAnterior = new Map(estado.agentes.map(function (a) {
        return [a.slug, a.status];
      }));
      renderLista(estado.agentes);
      scene?.applyEstado(estado);
    } catch {
      /* silent */
    }
  }

  async function main() {
    // agents.js may already be on page (desktop Scripts section) — panel setup after
    if (window.EscritorioUiPanel) setupPanel();
    wireA11y();
    applyViewMode();
    window.addEventListener("resize", applyViewMode);

    const estado = await fetchEstado();
    cachedEstado = estado;
    pollingSegundos = estado.pollingSegundos;
    statusAnterior = new Map(estado.agentes.map(function (a) {
      return [a.slug, a.status];
    }));

    if (window.EscritorioAgents) {
      renderLista(estado.agentes);
    }

    if (showingMap && window.innerWidth >= 720) {
      await ensurePhaserScripts();
      if (!panel) setupPanel();
      cachedManifest = await loadManifest();
      startPhaser(cachedManifest, estado);
      renderLista(estado.agentes);
    }

    timer = setInterval(poll, Math.max(5, pollingSegundos) * 1000);
    document.addEventListener("visibilitychange", function () {
      if (document.visibilityState === "visible") poll();
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", main);
  } else {
    main();
  }
})();
