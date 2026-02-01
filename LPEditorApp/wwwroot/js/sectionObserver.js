window.sectionObserver = {
  _instances: new Map(),
  setup: function (frameId, dotnetRef) {
    const iframe = document.getElementById(frameId);
    if (!iframe) return;

    const attach = () => {
      const doc = iframe.contentDocument;
      const win = iframe.contentWindow;
      if (!doc || !win) return;

      const sections = Array.from(doc.querySelectorAll("[data-section-id]"));
      if (!sections.length) return;

      const state = window.sectionObserver._instances.get(frameId) || {};
      if (state.observer) {
        state.observer.disconnect();
      }

      let currentId = null;
      let notifyRaf = null;

      const notify = (id) => {
        if (!id || id === currentId) return;
        currentId = id;
        if (dotnetRef && typeof dotnetRef.invokeMethodAsync === "function") {
          dotnetRef.invokeMethodAsync("OnPreviewSectionChanged", id);
        }
      };

      const pickBest = (entries) => {
        let best = null;
        let bestScore = 0;
        for (const entry of entries) {
          if (!entry.isIntersecting) continue;
          const rect = entry.boundingClientRect;
          const visible = Math.min(rect.bottom, win.innerHeight) - Math.max(rect.top, 0);
          const ratio = entry.intersectionRatio || 0;
          const coverage = Math.max(0, visible) / Math.max(1, rect.height || 1);
          const score = ratio * 0.7 + coverage * 0.3;
          if (score > bestScore) {
            bestScore = score;
            best = entry.target;
          }
        }
        if (!best) return;
        const id = best.getAttribute("data-section-id");
        if (!id) return;
        if (notifyRaf) return;
        notifyRaf = win.requestAnimationFrame(() => {
          notifyRaf = null;
          notify(id);
        });
      };

      const observer = new win.IntersectionObserver(pickBest, {
        root: null,
        threshold: [0, 0.25, 0.5, 0.75, 1],
        rootMargin: "0px 0px -35% 0px"
      });

      sections.forEach((section) => observer.observe(section));
      state.observer = observer;
      window.sectionObserver._instances.set(frameId, state);

      win.requestAnimationFrame(() => {
        let best = null;
        let bestScore = 0;
        sections.forEach((section) => {
          const rect = section.getBoundingClientRect();
          const visible = Math.min(rect.bottom, win.innerHeight) - Math.max(rect.top, 0);
          const ratio = Math.max(0, visible) / Math.max(1, rect.height || 1);
          if (ratio > bestScore) {
            bestScore = ratio;
            best = section;
          }
        });
        if (best) {
          notify(best.getAttribute("data-section-id"));
        }
      });
    };

    if (iframe.contentDocument && iframe.contentDocument.readyState === "complete") {
      attach();
    } else {
      iframe.addEventListener("load", attach, { once: true });
    }
  },
  scrollToSection: function (frameId, sectionId) {
    const iframe = document.getElementById(frameId);
    if (!iframe || !iframe.contentDocument) return;
    const doc = iframe.contentDocument;
    const target = doc.querySelector(`[data-section-id='${sectionId}']`);
    if (!target) return;
    target.scrollIntoView({ behavior: "smooth", block: "center" });
  },
  scrollRow: function (listId, rowId) {
    if (!listId || !rowId) return;
    const list = document.getElementById(listId);
    const row = document.getElementById(rowId);
    if (!list || !row) return;
    const listRect = list.getBoundingClientRect();
    const rowRect = row.getBoundingClientRect();
    if (rowRect.top < listRect.top || rowRect.bottom > listRect.bottom) {
      row.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }
  }
};
