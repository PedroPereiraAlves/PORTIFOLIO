(function () {
  "use strict";

  const ESTADO_URL = "/admin/escritorio/estado";
  const listaLive = document.getElementById("escritorioListaLive");
  const tbody = document.querySelector("#escritorioTabela tbody");
  const cards = document.getElementById("escritorioCards");
  const panel = document.getElementById("escritorioPanel");
  const panelTitle = document.getElementById("escritorioPanelTitle");
  const panelPapel = document.getElementById("escritorioPanelPapel");
  const panelStatus = document.getElementById("escritorioPanelStatus");
  const panelProjeto = document.getElementById("escritorioPanelProjeto");
  const panelQuando = document.getElementById("escritorioPanelQuando");
  const panelTarefa = document.getElementById("escritorioPanelTarefa");
  const panelPr = document.getElementById("escritorioPanelPr");
  const panelEntregas = document.getElementById("escritorioPanelEntregas");
  const panelClose = document.getElementById("escritorioPanelClose");

  /** @type {Map<string, string>} */
  let statusAnterior = new Map();
  /** @type {Map<string, object>} */
  let porSlug = new Map();
  let pollingSegundos = 20;
  let timer = null;

  function formatStatus(s) {
    return (s || "").replace(/-/g, " ");
  }

  function escapeHtml(s) {
    return String(s ?? "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function anunciarDiffs(agentes) {
    if (!listaLive) return;
    const msgs = [];
    for (const a of agentes) {
      const prev = statusAnterior.get(a.slug);
      if (prev && prev !== a.status) {
        msgs.push(a.nome + ": " + formatStatus(prev) + " → " + formatStatus(a.status));
      }
    }
    listaLive.textContent = msgs.length ? msgs.join(". ") : "";
  }

  function abrirPainel(slug) {
    const a = porSlug.get(slug);
    if (!a || !panel) return;
    panelTitle.textContent = a.nome;
    panelPapel.textContent = a.papel || "";
    panelStatus.textContent = formatStatus(a.status) + (a.desatualizado ? " (desatualizado)" : "");
    panelProjeto.textContent = a.projeto || "—";
    panelQuando.textContent = a.haQuantoTempo || "—";
    panelTarefa.textContent = a.tarefaAtual || "";
    if (a.prUrl) {
      panelPr.href = a.prUrl;
      panelPr.hidden = false;
    } else {
      panelPr.hidden = true;
    }
    panelEntregas.innerHTML = "";
    const entregas = a.ultimasEntregas || [];
    if (!entregas.length) {
      const li = document.createElement("li");
      li.className = "muted";
      li.textContent = "Sem entregas recentes.";
      panelEntregas.appendChild(li);
    } else {
      for (const e of entregas.slice(0, 5)) {
        const li = document.createElement("li");
        const titulo = escapeHtml(e.titulo);
        if (e.link) {
          li.innerHTML =
            "<span class=\"escritorio-entrega-tipo\">" +
            escapeHtml(e.tipo) +
            "</span> <a href=\"" +
            escapeHtml(e.link) +
            "\" target=\"_blank\" rel=\"noopener noreferrer\">" +
            titulo +
            "</a>";
        } else {
          li.innerHTML =
            "<span class=\"escritorio-entrega-tipo\">" +
            escapeHtml(e.tipo) +
            "</span> " +
            titulo;
        }
        panelEntregas.appendChild(li);
      }
    }
    panel.hidden = false;
    panelClose?.focus();
  }

  function fecharPainel() {
    if (!panel) return;
    panel.hidden = true;
  }

  function renderLista(agentes) {
    if (!tbody || !cards) return;
    tbody.innerHTML = "";
    cards.innerHTML = "";
    for (const a of agentes) {
      const tr = document.createElement("tr");
      tr.tabIndex = 0;
      tr.dataset.slug = a.slug;
      tr.innerHTML =
        "<td>" +
        escapeHtml(a.nome) +
        "</td><td>" +
        escapeHtml(a.sala) +
        "</td><td><span class=\"escritorio-status-chip status-" +
        escapeHtml(a.status) +
        "\">" +
        escapeHtml(formatStatus(a.status)) +
        "</span></td><td>" +
        escapeHtml(a.tarefaAtual || "—") +
        "</td><td>" +
        escapeHtml(a.haQuantoTempo || "—") +
        "</td>";
      tr.addEventListener("click", () => abrirPainel(a.slug));
      tr.addEventListener("keydown", (ev) => {
        if (ev.key === "Enter") {
          ev.preventDefault();
          abrirPainel(a.slug);
        }
      });
      tbody.appendChild(tr);

      const card = document.createElement("article");
      card.className = "admin-card";
      card.setAttribute("role", "listitem");
      card.tabIndex = 0;
      card.dataset.slug = a.slug;
      card.innerHTML =
        "<h3 class=\"admin-card-title\">" +
        escapeHtml(a.nome) +
        "</h3><p class=\"muted\">" +
        escapeHtml(a.sala) +
        " · " +
        escapeHtml(formatStatus(a.status)) +
        "</p><p>" +
        escapeHtml(a.tarefaAtual || "—") +
        "</p><p class=\"muted\">" +
        escapeHtml(a.haQuantoTempo || "") +
        "</p>";
      card.addEventListener("click", () => abrirPainel(a.slug));
      card.addEventListener("keydown", (ev) => {
        if (ev.key === "Enter") {
          ev.preventDefault();
          abrirPainel(a.slug);
        }
      });
      cards.appendChild(card);
    }
  }

  async function poll() {
    if (document.visibilityState === "hidden") return;
    try {
      const res = await fetch(ESTADO_URL, {
        headers: { Accept: "application/json" },
        credentials: "same-origin",
      });
      if (!res.ok) return;
      const data = await res.json();
      const agentes = (data.agentes || data.Agentes || []).map(normalize);
      pollingSegundos = data.pollingSegundos || data.PollingSegundos || 20;
      anunciarDiffs(agentes);
      porSlug = new Map(agentes.map((a) => [a.slug, a]));
      statusAnterior = new Map(agentes.map((a) => [a.slug, a.status]));
      renderLista(agentes);
    } catch {
      /* silent */
    }
  }

  function normalize(a) {
    return {
      slug: a.slug ?? a.Slug,
      nome: a.nome ?? a.Nome,
      papel: a.papel ?? a.Papel,
      sala: a.sala ?? a.Sala,
      status: a.status ?? a.Status,
      tarefaAtual: a.tarefaAtual ?? a.TarefaAtual,
      projeto: a.projeto ?? a.Projeto,
      prUrl: a.prUrl ?? a.PrUrl,
      haQuantoTempo: a.haQuantoTempo ?? a.HaQuantoTempo,
      desatualizado: a.desatualizado ?? a.Desatualizado,
      ultimasEntregas: a.ultimasEntregas ?? a.UltimasEntregas ?? [],
    };
  }

  function agendar() {
    if (timer) clearInterval(timer);
    timer = setInterval(poll, Math.max(5, pollingSegundos) * 1000);
  }

  panelClose?.addEventListener("click", fecharPainel);
  document.addEventListener("keydown", (ev) => {
    if (ev.key === "Escape" && panel && !panel.hidden) {
      fecharPainel();
    }
  });
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "visible") poll();
  });

  poll().then(agendar);
})();
