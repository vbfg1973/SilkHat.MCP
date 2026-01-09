(() => {
  let initialized = false;
  let currentThemeKey = null;

  function ensureInit(theme) {
    if (initialized || typeof mermaid === "undefined") {
      return;
    }

    mermaid.initialize({
      startOnLoad: false,
      theme: "neutral",
      securityLevel: "strict",
      themeVariables: theme || undefined,
    });
    initialized = true;
    currentThemeKey = theme ? JSON.stringify(theme) : "default";
  }

  function ensureTheme(theme) {
    if (typeof mermaid === "undefined") {
      return;
    }

    const themeKey = theme ? JSON.stringify(theme) : "default";
    if (!initialized || currentThemeKey !== themeKey) {
      mermaid.initialize({
        startOnLoad: false,
        theme: "neutral",
        securityLevel: "strict",
        themeVariables: theme || undefined,
      });
      initialized = true;
      currentThemeKey = themeKey;
    }
  }

  function waitForMermaid() {
    if (typeof mermaid !== "undefined") {
      return Promise.resolve(true);
    }

    let attempts = 0;
    return new Promise((resolve) => {
      const timer = setInterval(() => {
        attempts += 1;
        if (typeof mermaid !== "undefined") {
          clearInterval(timer);
          resolve(true);
          return;
        }

        if (attempts >= 10) {
          clearInterval(timer);
          resolve(false);
        }
      }, 100);
    });
  }

  async function waitForVisible(container) {
    let attempts = 0;
    while (attempts < 10) {
      const rect = container.getBoundingClientRect();
      if (rect.width > 0 && rect.height >= 0) {
        return true;
      }

      attempts += 1;
      await new Promise((resolve) => setTimeout(resolve, 100));
    }

    return false;
  }

  function buildThemeVariables(theme) {
    if (!theme) {
      return null;
    }

    return {
      primaryColor: theme.primary,
      primaryTextColor: theme.textPrimary,
      secondaryColor: theme.secondary,
      tertiaryColor: theme.tertiary || theme.secondary,
      mainBkg: theme.surface || theme.background,
      lineColor: theme.lines || theme.textSecondary,
      textColor: theme.textPrimary,
    };
  }

  async function render(containerId, diagram, theme) {
    const container = document.getElementById(containerId);
    if (!container) {
      return;
    }

    const ready = await waitForMermaid();
    await waitForVisible(container);
    const themeVariables = buildThemeVariables(theme);
    ensureInit(themeVariables);
    ensureTheme(themeVariables);
    if (!ready || typeof mermaid === "undefined") {
      container.textContent = diagram;
      return;
    }

    try {
      const renderId = `mmd-${containerId}-${Date.now()}`;
      const { svg } = await mermaid.render(renderId, diagram);
      if (svg && svg.trim().length > 0) {
        container.innerHTML = svg;
      } else {
        container.textContent = diagram;
        // eslint-disable-next-line no-console
        console.warn("Mermaid render returned empty SVG");
      }
    } catch (err) {
      container.textContent = diagram;
      // eslint-disable-next-line no-console
      console.warn("Mermaid render failed", err);
    }
  }

  function clear(containerId) {
    const container = document.getElementById(containerId);
    if (container) {
      container.innerHTML = "";
    }
  }

  window.silkhatMermaid = { render, clear };
})();
