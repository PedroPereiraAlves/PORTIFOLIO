(function (root) {
  "use strict";

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
      onClose,
      onOpen,
    } = els;

    function open(agente) {
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
      while (entregas.firstChild) entregas.removeChild(entregas.firstChild);
      const list = agente.ultimasEntregas || [];
      if (!list.length) {
        const li = document.createElement("li");
        li.className = "muted";
        li.textContent = "Sem entregas recentes.";
        entregas.appendChild(li);
      } else {
        for (const e of list.slice(0, 5)) {
          const li = document.createElement("li");
          const tip = document.createElement("span");
          tip.className = "escritorio-entrega-tipo";
          tip.textContent = e.tipo || e.Tipo || "";
          li.appendChild(tip);
          li.appendChild(document.createTextNode(" "));
          const link = e.link || e.Link;
          const tit = e.titulo || e.Titulo || "";
          if (link) {
            const a = document.createElement("a");
            a.href = link;
            a.target = "_blank";
            a.rel = "noopener noreferrer";
            a.textContent = tit;
            li.appendChild(a);
          } else {
            li.appendChild(document.createTextNode(tit));
          }
          entregas.appendChild(li);
        }
      }
      panel.hidden = false;
      panel.classList.add("is-open");
      onOpen && onOpen();
      closeBtn?.focus();
    }

    function close() {
      if (!panel) return;
      panel.hidden = true;
      panel.classList.remove("is-open");
      onClose && onClose();
    }

    closeBtn?.addEventListener("click", close);

    return { open, close, isOpen: () => panel && !panel.hidden };
  }

  root.EscritorioUiPanel = { createPanelController, formatStatus };
})(typeof self !== "undefined" ? self : this);
