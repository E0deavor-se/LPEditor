window.lpHighlight = {
  flash: function (frameId, sectionKey, kind) {
    const iframe = document.getElementById(frameId);
    if (!iframe || !iframe.contentDocument) return;
    const doc = iframe.contentDocument;
    let target = null;
    if (sectionKey) {
      target = doc.querySelector(`.section-group[data-section='${sectionKey}']`);
    }
    if (!target) {
      target = doc.body;
    }
    target.classList.add("lp-highlight");
    if (kind) {
      target.classList.add(`lp-highlight-${kind}`);
    }
    setTimeout(() => {
      target.classList.remove("lp-highlight");
      if (kind) {
        target.classList.remove(`lp-highlight-${kind}`);
      }
    }, 1400);
  },
  scrollToSection: function (frameId, sectionKey) {
    const iframe = document.getElementById(frameId);
    if (!iframe || !iframe.contentDocument) return;
    const doc = iframe.contentDocument;
    const target = doc.querySelector(`.section-group[data-section='${sectionKey}']`);
    if (!target) return;
    target.scrollIntoView({ behavior: "smooth", block: "center" });
  },
  setPreviewMode: function (frameId, mode) {
    const iframe = document.getElementById(frameId);
    if (!iframe || !iframe.contentDocument) return;
    const doc = iframe.contentDocument;
    doc.body.classList.toggle("lp-final-mode", mode === "final");
  },
  scrollToEditor: function (selector) {
    const target = document.querySelector(selector);
    if (!target) return;
    target.scrollIntoView({ behavior: "smooth", block: "center" });
  }
};
