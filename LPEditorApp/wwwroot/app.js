window.lpPreview = {
  setHtml: function (frameId, html) {
    const iframe = document.getElementById(frameId);
    if (!iframe) return;
    iframe.srcdoc = window.lpPreview._injectPreviewDebug(html || "");
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

    $container.sortable({
      items: ".item",
      cancel: "input, label, button, textarea, select",
      handle: ".drag-handle",
      placeholder: "drag-placeholder",
      axis: "y",
      opacity: 0.5,
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
