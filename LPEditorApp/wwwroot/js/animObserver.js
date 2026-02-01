(function () {
  if (document.documentElement.dataset.animObserverReady) return;
  document.documentElement.dataset.animObserverReady = "true";

  if (document.body && document.body.getAttribute("data-anim-disabled") === "true") {
    document.querySelectorAll(".anim-on-scroll").forEach((el) => el.classList.add("is-inview"));
    return;
  }

  const prefersReduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const items = Array.from(document.querySelectorAll(".anim-on-scroll"));
  if (items.length === 0) return;

  const reveal = (el) => el.classList.add("is-inview");

  if (prefersReduce || !("IntersectionObserver" in window)) {
    items.forEach(reveal);
    return;
  }

  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (!entry.isIntersecting) return;
      reveal(entry.target);
      observer.unobserve(entry.target);
    });
  }, { threshold: 0.25 });

  items.forEach((el) => observer.observe(el));
})();
