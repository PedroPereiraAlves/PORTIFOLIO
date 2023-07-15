function toggleText() {
    var shortText = document.getElementById("short-text");
    var fullText = document.getElementById("full-text");
    var seeMoreLink = document.getElementById("see-more");

    shortText.classList.toggle("hide");
    fullText.classList.toggle("hide");

    if (fullText.classList.contains("hide")) {
      seeMoreLink.textContent = "...ver mais";
    } else {
      seeMoreLink.textContent = "...ver menos";
    }
  }