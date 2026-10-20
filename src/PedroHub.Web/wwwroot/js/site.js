(() => {
  const root = document.documentElement;
  const themeKey = "pedrohung-theme";

  function currentTheme() {
    return root.getAttribute("data-theme") === "dark" ? "dark" : "light";
  }

  function applyTheme(theme, persist) {
    root.setAttribute("data-theme", theme);
    if (persist) {
      try {
        localStorage.setItem(themeKey, theme);
      } catch (_) {
        /* ignore quota / private mode */
      }
    }
    const toggle = document.getElementById("themeToggle");
    if (toggle) {
      toggle.setAttribute(
        "aria-label",
        theme === "dark" ? "Ativar tema claro" : "Ativar tema escuro"
      );
    }
  }

  if (!root.getAttribute("data-theme")) {
    let initial = "light";
    try {
      const saved = localStorage.getItem(themeKey);
      const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
      initial = saved || (prefersDark ? "dark" : "light");
    } catch (_) {
      initial = "light";
    }
    applyTheme(initial, false);
  } else {
    applyTheme(currentTheme(), false);
  }

  const themeToggle = document.getElementById("themeToggle");
  if (themeToggle) {
    themeToggle.addEventListener("click", () => {
      applyTheme(currentTheme() === "dark" ? "light" : "dark", true);
    });
  }

  const nav = document.getElementById("siteNav");
  const navToggle = document.getElementById("navToggle");

  function closeNav() {
    if (!nav || !navToggle) return;
    nav.classList.remove("is-open");
    navToggle.setAttribute("aria-expanded", "false");
    navToggle.setAttribute("aria-label", "Abrir menu de navegação");
  }

  function openNav() {
    if (!nav || !navToggle) return;
    nav.classList.add("is-open");
    navToggle.setAttribute("aria-expanded", "true");
    navToggle.setAttribute("aria-label", "Fechar menu de navegação");
  }

  if (nav && navToggle) {
    navToggle.addEventListener("click", () => {
      if (nav.classList.contains("is-open")) closeNav();
      else openNav();
    });

    nav.querySelectorAll("a, button").forEach((el) => {
      el.addEventListener("click", () => {
        if (el.closest("form.nav-logout")) return;
        closeNav();
      });
    });

    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape") closeNav();
    });
  }

  const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  if (!reduceMotion && "IntersectionObserver" in window) {
    const reveals = document.querySelectorAll(".reveal");
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("is-visible");
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.12, rootMargin: "0px 0px -40px 0px" }
    );
    reveals.forEach((el) => observer.observe(el));
  } else {
    document.querySelectorAll(".reveal").forEach((el) => el.classList.add("is-visible"));
  }

  document.querySelectorAll("form.sync-form, form.admin-side-form").forEach((form) => {
    form.addEventListener("submit", () => {
      const btn = form.querySelector("button[type='submit']");
      if (!btn || btn.disabled) return;
      const label = btn.getAttribute("data-loading-label") || "Aguarde…";
      btn.disabled = true;
      btn.classList.add("is-loading");
      btn.textContent = label;
    });
  });

  document.addEventListener(
    "submit",
    (event) => {
      const form = event.target;
      if (!(form instanceof HTMLFormElement)) return;
      const submitter = event.submitter;
      const msg =
        (submitter && submitter.getAttribute("data-confirm")) ||
        form.getAttribute("data-confirm");
      if (!msg) return;
      if (!window.confirm(msg)) {
        event.preventDefault();
        event.stopPropagation();
      }
    },
    true
  );
})();
