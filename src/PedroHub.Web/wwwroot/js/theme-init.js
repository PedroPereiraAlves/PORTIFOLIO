(function () {
  var root = document.documentElement;
  root.classList.add("js");

  var key = "pedrohung-theme";
  try {
    var saved = localStorage.getItem(key);
    var prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
    var theme = saved || (prefersDark ? "dark" : "light");
    root.setAttribute("data-theme", theme);
  } catch (e) {
    root.setAttribute("data-theme", "light");
  }
})();
