const articles = document.querySelectorAll("article");

articles.forEach((article) => {
  article.addEventListener("mouseenter", () => {
    article.querySelector(".article-content").style.transform = "translateY(0)";
  });

  article.addEventListener("mouseleave", () => {
    article.querySelector(".article-content").style.transform = "translateY(100%)";
  });
});
