(() => {
  const el = document.getElementById("dados-consumo");
  const canvas = document.getElementById("grafico-consumo");
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
      ctx.fillStyle = getComputedStyle(document.documentElement).getPropertyValue("--color-text-muted").trim() || "#5a6b80";
      ctx.font = "14px sans-serif";
      ctx.fillText("Sem uso neste período.", 16, 40);
    }
    return;
  }

  const styles = getComputedStyle(document.documentElement);
  const gold = styles.getPropertyValue("--color-gold").trim() || "#f5b700";
  const blue = styles.getPropertyValue("--color-blue").trim() || "#0b2e6f";
  const muted = styles.getPropertyValue("--color-text-muted").trim() || "#5a6b80";

  const dinheiro = (valor) => {
    if (valor == null) return "custo não informado";
    return "US$ " + Number(valor).toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  };

  new Chart(canvas, {
    type: "bar",
    data: {
      labels: pontos.map((p) => p.dia),
      datasets: [
        {
          label: "Tokens",
          data: pontos.map((p) => p.tokens),
          backgroundColor: gold,
          borderRadius: 4,
          maxBarThickness: 28,
        },
      ],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { labels: { color: muted } },
        tooltip: {
          callbacks: {
            label: (ctx) => {
              const p = pontos[ctx.dataIndex] || {};
              const tokens = Number(p.tokens || 0).toLocaleString("pt-BR");
              const reqs = Number(p.requisicoes || 0).toLocaleString("pt-BR");
              return [tokens + " tokens", reqs + " requisições", dinheiro(p.custoUsd)];
            },
          },
        },
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
          title: { display: true, text: "tokens", color: muted },
        },
      },
    },
  });
})();
