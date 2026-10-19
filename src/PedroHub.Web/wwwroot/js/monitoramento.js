(() => {
  const el = document.getElementById("dados-latencia");
  const canvas = document.getElementById("grafico-latencia");
  if (!el || !canvas || typeof Chart === "undefined") return;

  let pontos = [];
  try {
    pontos = JSON.parse(el.textContent || "[]");
  } catch (_) {
    return;
  }

  if (!Array.isArray(pontos) || pontos.length === 0) {
    const ctx = canvas.getContext("2d");
    if (ctx) {
      ctx.fillStyle = getComputedStyle(document.documentElement).getPropertyValue("--color-text-muted").trim() || "#888";
      ctx.font = "14px sans-serif";
      ctx.fillText("Sem dados de latência neste período.", 16, 40);
    }
    return;
  }

  const labels = pontos.map((p) => {
    const d = new Date(p.em);
    return d.toLocaleString("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });
  });
  const data = pontos.map((p) => p.latenciaMs);

  const styles = getComputedStyle(document.documentElement);
  const gold = styles.getPropertyValue("--color-gold").trim() || "#f5b700";
  const blue = styles.getPropertyValue("--color-blue").trim() || "#0b2e6f";
  const muted = styles.getPropertyValue("--color-text-muted").trim() || "#5a6b80";

  // eslint-disable-next-line no-new
  new Chart(canvas, {
    type: "line",
    data: {
      labels,
      datasets: [
        {
          label: "Latência (ms)",
          data,
          borderColor: gold,
          backgroundColor: "transparent",
          tension: 0.25,
          pointRadius: pontos.length > 40 ? 0 : 3,
          borderWidth: 2,
        },
      ],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { labels: { color: muted } },
      },
      scales: {
        x: {
          ticks: { color: muted, maxRotation: 0, autoSkip: true, maxTicksLimit: 8 },
          grid: { color: "transparent" },
        },
        y: {
          beginAtZero: true,
          ticks: { color: muted },
          grid: { color: blue + "22" },
          title: { display: true, text: "ms", color: muted },
        },
      },
    },
  });
})();
