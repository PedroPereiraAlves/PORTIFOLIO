(() => {
  const root = document.getElementById("escritorio-game");
  if (!root || typeof Phaser === "undefined") return;

  const listaLive = document.getElementById("escritorioListaLive");
  const tbody = document.querySelector("#escritorioTabela tbody");
  const cards = document.getElementById("escritorioCards");
  const panel = document.getElementById("escritorioPanel");
  const panelTitle = document.getElementById("escritorioPanelTitle");
  const panelStatus = document.getElementById("escritorioPanelStatus");
  const panelProjeto = document.getElementById("escritorioPanelProjeto");
  const panelQuando = document.getElementById("escritorioPanelQuando");
  const panelTarefa = document.getElementById("escritorioPanelTarefa");
  const panelPr = document.getElementById("escritorioPanelPr");
  const panelEntregas = document.getElementById("escritorioPanelEntregas");
  const panelClose = document.getElementById("escritorioPanelClose");
  const mapPane = document.getElementById("escritorioMapPane");
  const toggleView = document.getElementById("escritorioToggleView");

  let estado = { pollingSegundos: 20, agentes: [] };
  let selecionadoSlug = null;
  let lastFocusedSlug = null;
  let pollTimer = null;
  let game = null;
  let sceneRef = null;
  const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  function token(name, fallback) {
    const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    return v || fallback;
  }

  function corStatus(status) {
    switch (status) {
      case "trabalhando":
        return token("--color-blue", "#1e5a8a");
      case "revisando":
        return token("--color-gold", "#c9a227");
      case "bloqueado":
        return token("--color-error-text", "#b91c1c");
      case "pausado-sem-cota":
        return "#6b7280";
      default:
        return token("--color-text-muted", "#64748b");
    }
  }

  function abrirPainel(agente) {
    if (!agente || !panel) return;
    selecionadoSlug = agente.slug;
    lastFocusedSlug = agente.slug;
    panel.hidden = false;
    panelTitle.textContent = agente.nome;
    panelStatus.textContent = agente.status;
    panelStatus.style.color = corStatus(agente.status);
    panelProjeto.textContent = agente.projeto || "—";
    panelQuando.textContent = agente.haQuantoTempo || "—";
    panelTarefa.textContent = agente.tarefaAtual || "Sem tarefa publicada.";
    if (agente.prUrl) {
      panelPr.hidden = false;
      panelPr.href = agente.prUrl;
    } else {
      panelPr.hidden = true;
      panelPr.removeAttribute("href");
    }
    panelEntregas.replaceChildren();
    (agente.ultimasEntregas || []).forEach((e) => {
      const li = document.createElement("li");
      const titulo = document.createElement("strong");
      titulo.textContent = e.titulo;
      li.appendChild(titulo);
      if (e.descricao) {
        const d = document.createElement("span");
        d.className = "muted";
        d.textContent = " — " + e.descricao;
        li.appendChild(d);
      }
      if (e.link) {
        const a = document.createElement("a");
        a.href = e.link;
        a.target = "_blank";
        a.rel = "noopener noreferrer";
        a.textContent = " link";
        li.appendChild(a);
      }
      panelEntregas.appendChild(li);
    });
    if (sceneRef && sceneRef.highlightSlug) sceneRef.highlightSlug(agente.slug);
  }

  function fecharPainel() {
    if (!panel) return;
    panel.hidden = true;
    selecionadoSlug = null;
    if (sceneRef && sceneRef.highlightSlug) sceneRef.highlightSlug(null);
    if (lastFocusedSlug && sceneRef && sceneRef.focusSlug) {
      sceneRef.focusSlug(lastFocusedSlug);
    }
  }

  function renderLista() {
    if (!tbody || !cards) return;
    tbody.replaceChildren();
    cards.replaceChildren();
    const mudancas = [];
    estado.agentes.forEach((a) => {
      const tr = document.createElement("tr");
      tr.tabIndex = 0;
      tr.dataset.slug = a.slug;
      [
        a.nome,
        a.sala,
        a.status + (a.desatualizado ? " (desat.)" : ""),
        a.tarefaAtual || "—",
        a.haQuantoTempo || "—",
      ].forEach((text) => {
        const td = document.createElement("td");
        td.textContent = text;
        tr.appendChild(td);
      });
      tr.addEventListener("click", () => abrirPainel(a));
      tr.addEventListener("keydown", (ev) => {
        if (ev.key === "Enter" || ev.key === " ") {
          ev.preventDefault();
          abrirPainel(a);
        }
      });
      tbody.appendChild(tr);

      const card = document.createElement("article");
      card.className = "admin-card";
      card.setAttribute("role", "listitem");
      card.tabIndex = 0;
      const h = document.createElement("h3");
      h.className = "admin-card-title";
      h.textContent = a.nome;
      const meta = document.createElement("p");
      meta.className = "muted";
      meta.textContent = a.sala + " · " + a.status + " · " + (a.haQuantoTempo || "—");
      const tarefa = document.createElement("p");
      tarefa.textContent = a.tarefaAtual || "Sem tarefa.";
      card.appendChild(h);
      card.appendChild(meta);
      card.appendChild(tarefa);
      card.addEventListener("click", () => abrirPainel(a));
      card.addEventListener("keydown", (ev) => {
        if (ev.key === "Enter" || ev.key === " ") {
          ev.preventDefault();
          abrirPainel(a);
        }
      });
      cards.appendChild(card);
      mudancas.push(a.nome + ": " + a.status);
    });
    if (listaLive) listaLive.textContent = mudancas.join(" · ");
  }

  async function carregar() {
    try {
      const res = await fetch("/admin/escritorio/estado", {
        credentials: "same-origin",
        headers: { Accept: "application/json" },
      });
      if (!res.ok) return;
      const data = await res.json();
      estado = {
        pollingSegundos: data.pollingSegundos || 20,
        agentes: data.agentes || [],
      };
      renderLista();
      if (sceneRef && sceneRef.applyEstado) sceneRef.applyEstado(estado.agentes);
      if (selecionadoSlug) {
        const a = estado.agentes.find((x) => x.slug === selecionadoSlug);
        if (a) abrirPainel(a);
      }
    } catch (_) {
      /* rede / aba oculta */
    }
  }

  function agendarPoll() {
    if (pollTimer) clearInterval(pollTimer);
    const ms = Math.max(5, estado.pollingSegundos || 20) * 1000;
    pollTimer = setInterval(() => {
      if (document.visibilityState === "hidden") return;
      carregar();
    }, ms);
  }

  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "visible") carregar();
  });

  if (panelClose) panelClose.addEventListener("click", fecharPainel);
  document.addEventListener("keydown", (ev) => {
    if (ev.key === "Escape") fecharPainel();
  });

  function syncMobileView() {
    const mobile = window.matchMedia("(max-width: 719px)").matches;
    if (!toggleView || !mapPane) return;
    if (mobile) {
      toggleView.hidden = false;
      const showingMap = mapPane.classList.contains("is-visible-mobile");
      toggleView.textContent = showingMap ? "Ver lista" : "Ver mapa";
      if (!showingMap) mapPane.setAttribute("hidden", "hidden");
      else mapPane.removeAttribute("hidden");
    } else {
      toggleView.hidden = true;
      mapPane.removeAttribute("hidden");
      mapPane.classList.remove("is-visible-mobile");
    }
  }

  if (toggleView) {
    toggleView.addEventListener("click", () => {
      mapPane.classList.toggle("is-visible-mobile");
      syncMobileView();
      if (game) game.scale.refresh();
    });
  }
  window.addEventListener("resize", syncMobileView);
  syncMobileView();

  class EscritorioScene extends Phaser.Scene {
    constructor() {
      super("escritorio");
      this.bonecos = new Map();
    }

    preload() {
      /* texturas geradas em create */
    }

    create() {
      sceneRef = this;
      const w = 640;
      const h = 360;
      this.cameras.main.setBackgroundColor(token("--color-surface-alt", "#e8eef5"));

      const g = this.make.graphics({ x: 0, y: 0, add: false });
      g.fillStyle(0xd4c4a8, 1);
      g.fillRect(0, 0, 16, 16);
      g.lineStyle(1, 0xb8a88c, 1);
      g.strokeRect(0, 0, 16, 16);
      g.generateTexture("floor", 16, 16);
      g.clear();
      g.fillStyle(0x1e5a8a, 1);
      g.fillRect(4, 2, 8, 8);
      g.fillStyle(0xf5d0a9, 1);
      g.fillRect(5, 3, 6, 5);
      g.fillStyle(0x1e5a8a, 1);
      g.fillRect(3, 10, 10, 6);
      g.generateTexture("agent", 16, 16);
      g.destroy();

      for (let x = 0; x < w; x += 16) {
        for (let y = 0; y < h; y += 16) {
          this.add.image(x + 8, y + 8, "floor").setDepth(0);
        }
      }

      this.add
        .rectangle(160, 180, 200, 260, 0x2a5080, 0.35)
        .setStrokeStyle(2, 0x1e5a8a);
      this.add
        .rectangle(480, 180, 200, 260, 0x8a6a2a, 0.28)
        .setStrokeStyle(2, 0xc9a227);
      this.add
        .rectangle(320, 180, 90, 70, 0xc9a227, 0.45)
        .setStrokeStyle(2, 0x8a7020);

      this.add
        .text(160, 40, "HUB", {
          fontFamily: "monospace",
          fontSize: "12px",
          color: "#1e5a8a",
        })
        .setOrigin(0.5);
      this.add
        .text(480, 40, "CLINICA", {
          fontFamily: "monospace",
          fontSize: "12px",
          color: "#8a7020",
        })
        .setOrigin(0.5);
      this.add
        .text(320, 150, "GIDEON", {
          fontFamily: "monospace",
          fontSize: "10px",
          color: "#5a4a10",
        })
        .setOrigin(0.5);

      this.applyEstado(estado.agentes);
    }

    posicao(agente, indexInSala) {
      if (agente.sala === "Central" || agente.slug === "gideon") {
        return { x: 320, y: 200 };
      }
      const hub = agente.sala === "Hub";
      const baseX = hub ? 90 : 410;
      const col = indexInSala % 3;
      const row = Math.floor(indexInSala / 3);
      return { x: baseX + col * 56, y: 90 + row * 70 };
    }

    applyEstado(agentes) {
      const porSala = { Hub: 0, Clinica: 0, Central: 0 };
      const vivos = new Set();

      agentes.forEach((a) => {
        vivos.add(a.slug);
        const idx = porSala[a.sala] ?? 0;
        if (a.sala !== "Central") porSala[a.sala] = idx + 1;
        const pos = this.posicao(a, idx);
        let b = this.bonecos.get(a.slug);
        if (!b) {
          const img = this.add
            .image(pos.x, pos.y, "agent")
            .setScale(2)
            .setInteractive({ useHandCursor: true })
            .setDepth(2);
          img.setTint(Phaser.Display.Color.HexStringToColor(a.corAvatar || "#1e5a8a").color);
          const label = this.add
            .text(pos.x, pos.y + 20, a.nome, {
              fontFamily: "monospace",
              fontSize: "9px",
              color: "#1a1a1a",
              backgroundColor: "#ffffffcc",
              padding: { x: 2, y: 1 },
            })
            .setOrigin(0.5)
            .setDepth(3);
          const badge = this.add
            .circle(pos.x + 12, pos.y - 12, 5, 0x888888)
            .setDepth(4)
            .setStrokeStyle(1, 0xffffff);
          img.on("pointerdown", () => abrirPainel(a));
          img.on("pointerover", () => img.setScale(2.2));
          img.on("pointerout", () => img.setScale(2));
          b = { img, label, badge, data: a };
          this.bonecos.set(a.slug, b);

          if (!reduceMotion) {
            this.tweens.add({
              targets: img,
              y: pos.y - 3,
              duration: 900 + (idx % 3) * 120,
              yoyo: true,
              repeat: -1,
              ease: "Sine.easeInOut",
            });
          }
        } else {
          b.data = a;
          b.img.off("pointerdown");
          b.img.on("pointerdown", () => abrirPainel(a));
          b.label.setText(a.nome);
          b.img.setPosition(pos.x, pos.y);
          b.label.setPosition(pos.x, pos.y + 20);
          b.badge.setPosition(pos.x + 12, pos.y - 12);
        }

        b.badge.setFillStyle(Phaser.Display.Color.HexStringToColor(corStatus(a.status)).color);
        b.img.setAlpha(a.desatualizado ? 0.45 : 1);
        b.label.setAlpha(a.desatualizado ? 0.55 : 1);
      });

      for (const [slug, b] of this.bonecos) {
        if (!vivos.has(slug)) {
          b.img.destroy();
          b.label.destroy();
          b.badge.destroy();
          this.bonecos.delete(slug);
        }
      }
    }

    highlightSlug(slug) {
      for (const [s, b] of this.bonecos) {
        b.img.setScale(s === slug ? 2.4 : 2);
      }
    }

    focusSlug(slug) {
      const b = this.bonecos.get(slug);
      if (b) b.img.emit("pointerover");
    }
  }

  const config = {
    type: Phaser.AUTO,
    parent: "escritorio-game",
    width: 640,
    height: 360,
    pixelArt: true,
    backgroundColor: token("--color-surface-alt", "#e8eef5"),
    scale: {
      mode: Phaser.Scale.FIT,
      autoCenter: Phaser.Scale.CENTER_BOTH,
    },
    scene: EscritorioScene,
    banner: false,
  };

  game = new Phaser.Game(config);

  carregar().then(agendarPoll);
})();
