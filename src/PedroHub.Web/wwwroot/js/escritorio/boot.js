(function () {
  "use strict";

  const ESTADO_URL = "/admin/escritorio/estado";
  const MANIFEST_URL = "/lib/office/assets/manifest.json";

  const mapPane = document.getElementById("escritorioMapPane");
  const gameHost = document.getElementById("escritorio-game");
  const toggleView = document.getElementById("escritorioToggleView");
  const toggleMode = document.getElementById("escritorioToggleMode");
  const btnCenter = document.getElementById("escritorioCenter");
  const a11yHelp = document.getElementById("escritorioA11yHelp");

  let game = null;
  let scene = null;
  let panel = null;
  let pollingSegundos = 20;
  let timer = null;
  let showingMap = window.innerWidth >= 720;
  let lastFocusEl = null;

  const Agents = window.EscritorioAgents;

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
    });
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

  function startPhaser(manifest, estado) {
    if (!gameHost || typeof Phaser === "undefined") return;

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
      scale: {
        mode: Phaser.Scale.RESIZE,
        autoCenter: Phaser.Scale.CENTER_BOTH,
      },
      scene: [],
    });
    game.scene.add(
      "OfficeScene",
      OfficeScene,
      true,
      {
        manifest,
        estado,
        onSelect: function (agente, entry) {
          lastFocusEl = document.activeElement;
          panel.open(agente, { faceKey: entry?.charKey });
          const img = document.getElementById("escritorioPanelPortrait");
          if (img && entry?.charKey) {
            img.src = "/lib/office/assets/chars/agents.png";
            img.style.imageRendering = "pixelated";
            img.hidden = false;
          }
        },
        onReady: function (s) {
          scene = s;
        },
      }
    );
  }

  function applyViewMode() {
    const mobile = window.innerWidth < 720;
    if (toggleView) {
      toggleView.hidden = !mobile;
      toggleView.textContent = showingMap ? "Ver lista" : "Ver mapa";
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

  function wireA11y() {
    if (gameHost) {
      gameHost.tabIndex = 0;
      gameHost.setAttribute("role", "application");
      if (a11yHelp) gameHost.setAttribute("aria-describedby", "escritorioA11yHelp");
      gameHost.addEventListener("keydown", (ev) => {
        if (!scene) return;
        if (ev.key === "Tab") {
          ev.preventDefault();
          scene.selectByTab(ev.shiftKey ? -1 : 1);
        } else if (ev.key === "Enter") {
          if (scene.selectedSlug) scene.selectAgent(scene.selectedSlug);
          else if (scene.tabList?.[0]) scene.selectAgent(scene.tabList[0]);
        } else if (ev.key === "Escape") {
          panel?.close();
          if (lastFocusEl) lastFocusEl.focus();
          else gameHost.focus();
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
    toggleView?.addEventListener("click", () => {
      showingMap = !showingMap;
      applyViewMode();
    });
    toggleMode?.addEventListener("click", () => {
      showingMap = !showingMap;
      applyViewMode();
      toggleMode.textContent = showingMap ? "Lista" : "Mapa";
    });
    btnCenter?.addEventListener("click", () => scene?.centerMap());
    document.addEventListener("keydown", (ev) => {
      if (ev.key === "Escape" && panel?.isOpen()) {
        panel.close();
        gameHost?.focus();
      }
    });
  }

  // list rendering + aria-live diffs (reuse)
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
        msgs.push(a.nome + ": " + prev.replace(/-/g, " ") + " → " + a.status.replace(/-/g, " "));
      }
    }
    listaLive.textContent = msgs.length ? msgs.join(". ") : "";
  }

  function renderLista(agentes) {
    if (!tbody || !cards) return;
    const ordered = Agents.tabOrder(agentes);
    tbody.innerHTML = "";
    cards.innerHTML = "";
    for (const a of ordered) {
      const tr = document.createElement("tr");
      tr.tabIndex = 0;
      tr.dataset.slug = a.slug;
      tr.innerHTML =
        "<td>" +
        a.nome +
        "</td><td>" +
        a.sala +
        '</td><td><span class="escritorio-status-chip status-' +
        a.status +
        '">' +
        a.status.replace(/-/g, " ") +
        "</span></td><td>" +
        (a.tarefaAtual || "—") +
        "</td><td>" +
        (a.haQuantoTempo || "—") +
        "</td>";
      tr.addEventListener("click", () => {
        scene?.selectAgent(a.slug);
        panel.open(a, {});
      });
      tr.addEventListener("keydown", (ev) => {
        if (ev.key === "Enter") {
          scene?.selectAgent(a.slug);
          panel.open(a, {});
        }
      });
      tbody.appendChild(tr);

      const card = document.createElement("article");
      card.className = "admin-card";
      card.tabIndex = 0;
      card.innerHTML =
        '<h3 class="admin-card-title">' +
        a.nome +
        '</h3><p class="muted">' +
        a.sala +
        " · " +
        a.status.replace(/-/g, " ") +
        "</p>";
      card.addEventListener("click", () => {
        scene?.selectAgent(a.slug);
        panel.open(a, {});
      });
      cards.appendChild(card);
    }
  }

  async function poll() {
    if (document.visibilityState === "hidden") return;
    try {
      const estado = await fetchEstado();
      pollingSegundos = estado.pollingSegundos;
      anunciarDiffs(estado.agentes);
      statusAnterior = new Map(estado.agentes.map((a) => [a.slug, a.status]));
      renderLista(estado.agentes);
      scene?.applyEstado(estado);
    } catch {
      /* silent */
    }
  }

  async function main() {
    setupPanel();
    wireA11y();
    applyViewMode();
    window.addEventListener("resize", applyViewMode);

    const [manifest, estado] = await Promise.all([loadManifest(), fetchEstado()]);
    pollingSegundos = estado.pollingSegundos;
    statusAnterior = new Map(estado.agentes.map((a) => [a.slug, a.status]));
    renderLista(estado.agentes);

    if (showingMap || window.innerWidth >= 720) {
      startPhaser(manifest, estado);
    }

    timer = setInterval(poll, Math.max(5, pollingSegundos) * 1000);
    document.addEventListener("visibilitychange", () => {
      if (document.visibilityState === "visible") poll();
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", main);
  } else {
    main();
  }
})();
