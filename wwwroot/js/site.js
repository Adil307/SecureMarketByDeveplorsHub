(function () {
  const alerts = document.querySelectorAll(".alert");
  for (const alert of alerts) {
    window.setTimeout(function () {
      alert.style.transition = "opacity .25s ease";
      alert.style.opacity = "0";
    }, 3500);
  }
})();
