(() => {
  const DEFAULT_DURATION = 320;
  const DEFAULT_DEBOUNCE = 100;
  const STYLE_CLASSES = ["lp-hl-wash", "lp-hl-glow", "lp-hl-outline", "lp-hl-pulse"];
  const pending = new Map();
  const activeTimers = new WeakMap();

  const escapeSelector = (value) => {
    if (!value) return "";
    if (window.CSS && typeof window.CSS.escape === "function") {
      return window.CSS.escape(value);
    }
    return value.replace(/([#.;?+*~\[\]()'"\s])/g, "\\$1");
  };

  const buildSelector = (scopeType, id) => {
    if (!scopeType || scopeType === "page") {
      return "[data-hl-scope='page']";
    }
    if (id) {
      const safeId = escapeSelector(id);
      return `[data-hl-scope='${scopeType}'][data-hl-id='${safeId}']`;
    }
    return `[data-hl-scope='${scopeType}']`;
  };

  const applyHighlightToElement = (element, style, durationMs) => {
    STYLE_CLASSES.forEach((cls) => element.classList.remove(cls));
    element.classList.add("lp-hl");
    element.classList.add(`lp-hl-${style}`);

    const existing = activeTimers.get(element);
    if (existing) {
      clearTimeout(existing);
    }

    const timer = setTimeout(() => {
      element.classList.remove("lp-hl");
      STYLE_CLASSES.forEach((cls) => element.classList.remove(cls));
      activeTimers.delete(element);
    }, durationMs);
    activeTimers.set(element, timer);
  };

  const applyHighlightInDocument = (doc, request) => {
    if (!doc || !request) return;
    const scopeType = request.scopeType || "page";
    const style = request.style || "wash";
    const durationMs = Number.isFinite(request.durationMs) && request.durationMs > 0 ? request.durationMs : DEFAULT_DURATION;
    const selector = buildSelector(scopeType, request.id);
    const targets = selector ? Array.prototype.slice.call(doc.querySelectorAll(selector)) : [];

    if (targets.length === 0 && scopeType === "page" && doc.body) {
      applyHighlightToElement(doc.body, style, durationMs);
      return;
    }

    targets.forEach((target) => applyHighlightToElement(target, style, durationMs));
  };

  const debounceApply = (doc, request) => {
    if (!doc || !request) return;
    const debounceMs = Number.isFinite(request.debounceMs) && request.debounceMs >= 0 ? request.debounceMs : DEFAULT_DEBOUNCE;
    const key = `${request.scopeType || "page"}|${request.id || ""}|${request.style || "wash"}`;
    const existing = pending.get(key);
    if (existing) {
      clearTimeout(existing);
    }
    pending.set(
      key,
      setTimeout(() => {
        pending.delete(key);
        applyHighlightInDocument(doc, request);
      }, debounceMs)
    );
  };

  window.lpHighlight = {
    request: function (frameId, request) {
      if (!request) return;
      const iframe = frameId ? document.getElementById(frameId) : null;
      if (iframe && iframe.contentWindow) {
        try {
          iframe.contentWindow.postMessage({ type: "lp-highlight", payload: request }, "*");
          return;
        } catch (e) {
          // fall through to direct apply
        }
      }
      debounceApply(document, request);
    },
    flash: function (frameId, sectionKey, kind) {
      const mapped = {
        scopeType: kind === "background" ? "page" : "section",
        id: kind === "background" ? null : sectionKey || null,
        style: kind === "decor" ? "outline" : kind === "background" ? "wash" : "glow",
        durationMs: 320
      };
      window.lpHighlight.request(frameId, mapped);
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
})();
