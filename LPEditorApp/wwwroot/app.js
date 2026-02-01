window.lpPreview = {
  setHtml: function (frameId, html) {
    const iframe = document.getElementById(frameId);
    if (!iframe) return;
    iframe.srcdoc = window.lpPreview._injectPreviewDebug(html || "");
  },
  setHtmlWithScroll: function (frameId, html, scrollTop) {
    const iframe = document.getElementById(frameId);
    if (!iframe) return;
    const nextScroll = Number.isFinite(scrollTop) ? scrollTop : 0;
    const restore = () => {
      try {
        const doc = iframe.contentDocument;
        if (!doc) return;
        doc.documentElement.scrollTop = nextScroll;
        doc.body && (doc.body.scrollTop = nextScroll);
      } catch (e) {
        return;
      }
    };
    iframe.onload = restore;
    iframe.srcdoc = window.lpPreview._injectPreviewDebug(html || "");
  },
  getScrollTop: function (frameId) {
    const iframe = document.getElementById(frameId);
    if (!iframe || !iframe.contentDocument) return 0;
    const doc = iframe.contentDocument;
    return doc.documentElement.scrollTop || (doc.body ? doc.body.scrollTop : 0) || 0;
  },
  setScrollTop: function (frameId, value) {
    const iframe = document.getElementById(frameId);
    if (!iframe || !iframe.contentDocument) return;
    const doc = iframe.contentDocument;
    const next = Number.isFinite(value) ? value : 0;
    doc.documentElement.scrollTop = next;
    doc.body && (doc.body.scrollTop = next);
  },
  registerDebugListener: function () {
    if (window.__lpPreviewDebugListenerRegistered) return;
    window.__lpPreviewDebugListenerRegistered = true;
    window.addEventListener("message", (event) => {
      const data = event && event.data;
      if (!data || data.type !== "lp-preview-viewport") return;
      console.info("[LP Preview] iframe viewport", {
        innerWidth: data.innerWidth,
        innerHeight: data.innerHeight,
        matchMax767: data.matchMax767
      });
    });
  },
  _injectPreviewDebug: function (html) {
    if (!html) return html;

    const metaTag = "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">";
    const debugScript = "<script>(function(){try{function ensureBox(){var box=document.getElementById('lp-preview-debug');if(!box){box=document.createElement('div');box.id='lp-preview-debug';box.style.cssText='position:fixed;bottom:8px;right:8px;z-index:999999;background:rgba(15,23,42,0.8);color:#fff;font:12px/1.4 system-ui;padding:6px 8px;border-radius:6px;pointer-events:none';document.body.appendChild(box);}return box;}function emit(){var data={type:'lp-preview-viewport',innerWidth:window.innerWidth,innerHeight:window.innerHeight,matchMax767:window.matchMedia(\'(max-width: 767px)\').matches};var box=ensureBox();box.textContent='W:'+data.innerWidth+' H:'+data.innerHeight+' max767:'+data.matchMax767;window.parent&&window.parent.postMessage(data,'*');}emit();window.addEventListener('resize',emit);}catch(e){}})();<\/script>";

    let output = html;
    if (!/meta[^>]+name=["']viewport["']/i.test(output)) {
      if (/<head[^>]*>/i.test(output)) {
        output = output.replace(/<head[^>]*>/i, (m) => m + metaTag);
      } else {
        output = metaTag + output;
      }
    }

    if (/<body[^>]*>/i.test(output)) {
      output = output.replace(/<body[^>]*>/i, (m) => m + debugScript);
    } else {
      output = output + debugScript;
    }

    return output;
  },
  debugViewport: function (frameId, wrapperId) {
    const iframe = document.getElementById(frameId);
    const wrapper = wrapperId ? document.getElementById(wrapperId) : null;
    if (!iframe) return;

    const log = () => {
      const doc = iframe.contentDocument;
      const win = iframe.contentWindow;
      if (!doc || !win) {
        console.warn("[LP Preview] iframe document not ready");
        return;
      }

      const rect = iframe.getBoundingClientRect();
      const wrapperRect = wrapper ? wrapper.getBoundingClientRect() : null;
      const innerWidth = win.innerWidth;
      const clientWidth = doc.documentElement?.clientWidth || 0;
      const maxWidth600 = win.matchMedia("(max-width: 600px)").matches;
      const maxDeviceWidth600 = win.matchMedia("(max-device-width: 600px)").matches;
      const sourceMedias = Array.from(doc.querySelectorAll("source[media]")).slice(0, 5).map((s) => s.getAttribute("media"));
      const spImgCount = doc.querySelectorAll("img[src*='mv_sp'], source[srcset*='mv_sp']").length;

      console.info("[LP Preview] viewport", {
        innerWidth,
        clientWidth,
        maxWidth600,
        maxDeviceWidth600,
        iframeRectWidth: rect.width,
        wrapperRectWidth: wrapperRect ? wrapperRect.width : null
      });
      console.info("[LP Preview] media sources (first 5)", sourceMedias);
      console.info("[LP Preview] mv_sp count", spImgCount);
    };

    if (iframe.contentDocument?.readyState === "complete") {
      log();
    } else {
      iframe.addEventListener("load", log, { once: true });
    }
  },
  getFontSize: function (frameId, selector) {
    const iframe = document.getElementById(frameId);
    if (!iframe) return 0;

    const readSize = () => {
      const doc = iframe.contentDocument;
      const el = doc && doc.querySelector(selector);
      if (!el) return 0;
      const size = window.getComputedStyle(el).fontSize || "";
      const value = parseInt(size.replace("px", ""), 10);
      return Number.isFinite(value) ? value : 0;
    };

    return new Promise((resolve) => {
      const resolved = readSize();
      if (resolved > 0) {
        resolve(resolved);
        return;
      }

      const onLoad = () => resolve(readSize());
      iframe.addEventListener("load", onLoad, { once: true });
      setTimeout(() => resolve(readSize()), 150);
    });
  },
  setResponsiveViewport: function (wrapperId, frameId, width, height, dpr) {
    const wrapper = document.getElementById(wrapperId);
    const iframe = document.getElementById(frameId);
    if (!wrapper || !iframe) return;

    const applyDprStyle = () => {
      const doc = iframe.contentDocument;
      if (!doc) return false;
      const head = doc.head || doc.getElementsByTagName("head")[0];
      if (!head) return false;

      const existing = doc.querySelector("style[data-simulated-dpr='true']");
      if (existing) {
        existing.remove();
      }

      const safeDpr = Math.max(1, Number(dpr) || 1);
      if (safeDpr === 1) {
        return true;
      }

      const style = doc.createElement("style");
      style.setAttribute("data-simulated-dpr", "true");
      style.textContent = `html { transform: scale(${(1 / safeDpr).toFixed(4)}); transform-origin: top left; width: ${safeDpr * 100}%; height: ${safeDpr * 100}%; }`;
      head.appendChild(style);
      return true;
    };

    const apply = () => {
      iframe.style.width = width + "px";
      iframe.style.height = height + "px";

      const style = window.getComputedStyle(wrapper);
      const padX = parseFloat(style.paddingLeft || "0") + parseFloat(style.paddingRight || "0");
      const availableWidth = Math.max(0, wrapper.clientWidth - padX);
      const scale = Math.min(1, availableWidth / width);

      wrapper.style.setProperty("--preview-scale", String(scale));
      wrapper.style.setProperty("--preview-scaled-width", (width * scale).toFixed(2) + "px");
      wrapper.style.setProperty("--preview-scaled-height", (height * scale).toFixed(2) + "px");
      wrapper.dataset.responsiveWidth = width.toString();
      wrapper.dataset.responsiveHeight = height.toString();
      wrapper.dataset.responsiveDpr = Math.max(1, Number(dpr) || 1).toString();
    };

    if (!applyDprStyle()) {
      iframe.addEventListener("load", applyDprStyle, { once: true });
    }

    apply();

    if (!wrapper._responsiveObserver) {
      wrapper._responsiveObserver = new ResizeObserver(() => apply());
      wrapper._responsiveObserver.observe(wrapper);
    }
  },
  resetResponsiveViewport: function (frameId) {
    const iframe = document.getElementById(frameId);
    if (!iframe) return;
    const doc = iframe.contentDocument;
    if (doc) {
      doc.querySelector("style[data-simulated-dpr='true']")?.remove();
    }
    iframe.style.transform = "";
    iframe.style.transformOrigin = "";
    iframe.style.width = "";
    iframe.style.height = "";
    const wrapper = document.getElementById("previewFrameWrapper");
    if (wrapper) {
      wrapper.style.removeProperty("--preview-scale");
      wrapper.style.removeProperty("--preview-scaled-width");
      wrapper.style.removeProperty("--preview-scaled-height");
    }
  }
};

window.lpDesignColors = {
  load: function () {
    try {
      const raw = window.localStorage.getItem("lp-design-colors");
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed : [];
    } catch (e) {
      return [];
    }
  },
  save: function (colors) {
    try {
      const list = Array.isArray(colors) ? colors.slice(0, 8) : [];
      window.localStorage.setItem("lp-design-colors", JSON.stringify(list));
    } catch (e) {
      return;
    }
  }
};

window.lpCardColors = {
  load: function () {
    try {
      const raw = window.localStorage.getItem("lp-card-colors");
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed : [];
    } catch (e) {
      return [];
    }
  },
  save: function (colors) {
    try {
      const list = Array.isArray(colors) ? colors.slice(0, 8) : [];
      window.localStorage.setItem("lp-card-colors", JSON.stringify(list));
    } catch (e) {
      return;
    }
  }
};

window.lpFramePresets = {
  loadFavorites: function () {
    try {
      const raw = window.localStorage.getItem("lp-frame-preset-favorites");
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed : [];
    } catch (e) {
      return [];
    }
  },
  saveFavorites: function (items) {
    try {
      const list = Array.isArray(items) ? items : [];
      window.localStorage.setItem("lp-frame-preset-favorites", JSON.stringify(list));
    } catch (e) {
      return;
    }
  }
};

window.lpBackgroundPresets = {
  loadFavorites: function () {
    try {
      const raw = window.localStorage.getItem("lp-background-preset-favorites");
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed : [];
    } catch (e) {
      return [];
    }
  },
  saveFavorites: function (items) {
    try {
      const list = Array.isArray(items) ? items : [];
      window.localStorage.setItem("lp-background-preset-favorites", JSON.stringify(list));
    } catch (e) {
      return;
    }
  }
};


window.sectionSort = {
  _instances: new Map(),
  setup: function (containerId, dotnetRef) {
    const container = document.getElementById(containerId);
    if (!container) return;
    if (typeof window.jQuery === "undefined" || typeof window.jQuery.fn.sortable === "undefined") {
      console.warn("sectionSort: jQuery UI sortable is not available.");
      return;
    }

    const $container = window.jQuery(container);
    const existing = window.sectionSort._instances.get(containerId);
    if (existing) {
      existing.sortable("destroy");
      window.sectionSort._instances.delete(containerId);
    }

    let lastPointerY = 0;
    let scrollRaf = null;
    const scrollEdge = 40;
    const scrollSpeed = 14;

    const scheduleAutoScroll = () => {
      if (scrollRaf) return;
      scrollRaf = window.requestAnimationFrame(() => {
        scrollRaf = null;
        const rect = container.getBoundingClientRect();
        if (!rect.height) return;
        if (lastPointerY < rect.top + scrollEdge) {
          container.scrollTop -= scrollSpeed;
        } else if (lastPointerY > rect.bottom - scrollEdge) {
          container.scrollTop += scrollSpeed;
        }
      });
    };

    $container.sortable({
      items: ".item",
      cancel: "input, label, button, textarea, select",
      handle: ".drag-handle",
      placeholder: "drag-placeholder-line",
      forcePlaceholderSize: true,
      axis: "y",
      tolerance: "pointer",
      helper: function (event, ui) {
        const helper = ui.clone();
        helper.addClass("drag-ghost");
        helper.width(ui.outerWidth());
        return helper;
      },
      start: function (event, ui) {
        ui.item.addClass("is-dragging");
      },
      stop: function (event, ui) {
        ui.item.removeClass("is-dragging");
      },
      sort: function (event) {
        const origin = event.originalEvent || event;
        lastPointerY = origin && typeof origin.clientY === "number" ? origin.clientY : lastPointerY;
        scheduleAutoScroll();
      },
      update: function () {
        const order = $container.find(".item")
          .map(function () { return window.jQuery(this).attr("data-id"); })
          .get()
          .filter(Boolean);
        if (dotnetRef && typeof dotnetRef.invokeMethodAsync === "function") {
          dotnetRef.invokeMethodAsync("OnSectionOrderChanged", order);
        }
      }
    });

    $container.disableSelection();
    window.sectionSort._instances.set(containerId, $container);
  }
};

window.sectionNav = {
  _instances: new Map(),
  setup: function (frameId, dotnetRef) {
    const iframe = document.getElementById(frameId);
    if (!iframe) return;

    const attach = () => {
      const doc = iframe.contentDocument;
      const win = iframe.contentWindow;
      if (!doc || !win) return;

      const sections = Array.from(doc.querySelectorAll(".section-group[data-section]"));
      if (!sections.length) return;

      const state = window.sectionNav._instances.get(frameId) || {};
      state.sections = sections;
      state.win = win;
      state.dotnetRef = dotnetRef;

      if (state.onScroll) {
        doc.removeEventListener("scroll", state.onScroll, { passive: true });
      }

      let rafId = null;
      const handle = () => {
        rafId = null;
        let best = null;
        let bestScore = Number.POSITIVE_INFINITY;
        for (const section of sections) {
          const rect = section.getBoundingClientRect();
          const score = Math.abs(rect.top);
          if (score < bestScore) {
            bestScore = score;
            best = section;
          }
        }
        if (!best) return;
        const key = best.getAttribute("data-section");
        if (!key) return;
        if (dotnetRef && typeof dotnetRef.invokeMethodAsync === "function") {
          dotnetRef.invokeMethodAsync("OnPreviewSectionChanged", key);
        }
      };

      state.onScroll = () => {
        if (rafId) return;
        rafId = win.requestAnimationFrame(handle);
      };

      doc.addEventListener("scroll", state.onScroll, { passive: true });
      window.sectionNav._instances.set(frameId, state);
      state.onScroll();
    };

    if (iframe.contentDocument && iframe.contentDocument.readyState === "complete") {
      attach();
    } else {
      iframe.addEventListener("load", attach, { once: true });
    }
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

window.lpSplit = {
  init: function (containerId, previewId, splitterId, dotnetRef) {
    const container = document.getElementById(containerId);
    const preview = document.getElementById(previewId);
    const splitter = document.getElementById(splitterId);
    if (!container || !preview || !splitter) return false;

    const minWidth = 360;
    const minEditor = 420;
    let lastWidth = preview.getBoundingClientRect().width || 520;

    const applyWidth = (width) => {
      const maxWidth = Math.max(minWidth, window.innerWidth - minEditor - 40);
      const clamped = Math.max(minWidth, Math.min(maxWidth, width));
      lastWidth = clamped;
      preview.style.width = clamped + "px";
      preview.style.flex = `0 0 ${clamped}px`;
      if (dotnetRef && typeof dotnetRef.invokeMethodAsync === "function") {
        dotnetRef.invokeMethodAsync("OnPreviewWidthChanged", Math.round(clamped));
      }
    };

    applyWidth(lastWidth);

    let dragging = false;
    splitter.addEventListener("pointerdown", (e) => {
      dragging = true;
      splitter.setPointerCapture(e.pointerId);
      splitter.classList.add("is-dragging");
    });

    splitter.addEventListener("pointermove", (e) => {
      if (!dragging) return;
      const rect = container.getBoundingClientRect();
      const width = rect.right - e.clientX;
      applyWidth(width);
    });

    const endDrag = () => {
      dragging = false;
      splitter.classList.remove("is-dragging");
    };

    splitter.addEventListener("pointerup", endDrag);
    splitter.addEventListener("pointercancel", endDrag);

    const onResize = () => applyWidth(lastWidth);
    window.addEventListener("resize", onResize);

    return true;
  },
  setMode: function (containerId, previewId, isMobile) {
    const container = document.getElementById(containerId);
    const preview = document.getElementById(previewId);
    if (!container || !preview) return;

    const minWidth = 360;
    const minEditor = 420;
    const target = isMobile ? 440 : 1080;
    const maxWidth = Math.max(minWidth, window.innerWidth - minEditor - 40);
    const clamped = Math.max(minWidth, Math.min(maxWidth, target));
    preview.style.width = clamped + "px";
    preview.style.flex = `0 0 ${clamped}px`;
  }
  ,setPreviewWidth: function (containerId, previewId, width) {
    const container = document.getElementById(containerId);
    const preview = document.getElementById(previewId);
    if (!container || !preview) return;

    const minWidth = 360;
    const minEditor = 420;
    const maxWidth = Math.max(minWidth, window.innerWidth - minEditor - 40);
    const clamped = Math.max(minWidth, Math.min(maxWidth, width));
    preview.style.width = clamped + "px";
    preview.style.flex = `0 0 ${clamped}px`;
  }
};

window.lpExport = {
  saveZip: async function (suggestedName, bytes) {
    const safeName = suggestedName && suggestedName.trim() ? suggestedName.trim() : "lp-output.zip";
    const data = bytes instanceof Uint8Array ? bytes : new Uint8Array(bytes || []);

    try {
      if (window.showSaveFilePicker) {
        const handle = await window.showSaveFilePicker({
          suggestedName: safeName,
          types: [
            {
              description: "ZIP",
              accept: { "application/zip": [".zip"] }
            }
          ]
        });

        const writable = await handle.createWritable();
        await writable.write(data);
        await writable.close();
        return true;
      }

      const blob = new Blob([data], { type: "application/zip" });
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = safeName;
      anchor.style.display = "none";
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
      return true;
    } catch (e) {
      if (e && (e.name === "AbortError" || e.code === 20)) {
        return false;
      }
      console.error("lpExport.saveZip failed", e);
      throw e;
    }
  }
};
