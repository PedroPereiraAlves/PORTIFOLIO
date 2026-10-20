(function (root) {
  "use strict";

  function escapeHtml(s) {
    return String(s ?? "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function formatStatus(s) {
    return (s || "").replace(/-/g, " ");
  }

  function createPanelController(els) {
    const {
      panel,
      title,
      papel,
      status,
      projeto,
      quando,
      tarefa,
      pr,
      entregas,
      closeBtn,
      portrait,
    } = els;

    function open(agente, opts) {
      if (!panel || !agente) return;
      title.textContent = agente.nome;
      if (papel) papel.textContent = agente.papel || "";
      status.textContent =
        formatStatus(agente.status) + (agente.desatualizado ? " (desatualizado)" : "");
      status.className = "escritorio-status-chip status-" + (agente.status || "");
      projeto.textContent = agente.projeto || "—";
      quando.textContent = agente.haQuantoTempo || "—";
      tarefa.textContent = agente.tarefaAtual || "";
      if (pr) {
        if (agente.prUrl) {
          pr.href = agente.prUrl;
          pr.hidden = false;
        } else {
          pr.hidden = true;
        }
      }
      if (portrait) {
        const faceKey = (opts && opts.faceKey) || null;
        if (faceKey && opts && opts.faceUrl) {
          portrait.src = opts.faceUrl;
          portrait.hidden = false;
          portrait.alt = "Retrato de " + agente.nome;
        } else if (opts && opts.faceDataUrl) {
          portrait.src = opts.faceDataUrl;
          portrait.hidden = false;
        } else {
          portrait.hidden = true;
        }
      }
      entregas.innerHTML = "";
      const list = agente.ultimasEntregas || [];
      if (!list.length) {
        const li = document.createElement("li");
        li.className = "muted";
        li.textContent = "Sem entregas recentes.";
        entregas.appendChild(li);
      } else {
        for (const e of list.slice(0, 5)) {
          const li = document.createElement("li");
          const tip = escapeHtml(e.tipo || e.Tipo || "");
          const tit = escapeHtml(e.titulo || e.Titulo || "");
          const link = e.link || e.Link;
          li.innerHTML = link
            ? '<span class="escritorio-entrega-tipo">' +
              tip +
              '</span> <a href="' +
              escapeHtml(link) +
              '" target="_blank" rel="noopener noreferrer">' +
              tit +
              "</a>"
            : '<span class="escritorio-entrega-tipo">' + tip + "</span> " + tit;
          entregas.appendChild(li);
        }
      }
      panel.hidden = false;
      panel.classList.add("is-open");
      closeBtn?.focus();
    }

    function close() {
      if (!panel) return;
      panel.hidden = true;
      panel.classList.remove("is-open");
    }

    closeBtn?.addEventListener("click", close);

    return { open, close, isOpen: () => panel && !panel.hidden };
  }

  root.EscritorioUiPanel = { createPanelController, formatStatus, escapeHtml };
})(typeof self !== "undefined" ? self : this);
