(() => {
    const registry = new Map();

    function notify(dotNetRef) {
        if (!dotNetRef) {
            return;
        }

        try {
            dotNetRef.invokeMethodAsync("NotifyRenderRequested");
        } catch (err) {
            // eslint-disable-next-line no-console
            console.warn("Visualization host notify failed", err);
        }
    }

    let visibilityListenerAdded = false;

    function register(containerId, dotNetRef) {
        const element = document.getElementById(containerId);
        if (!element) {
            return;
        }

        unregister(containerId);

        const state = {
            element,
            dotNetRef,
            resizeObserver: null,
            intersectionObserver: null,
        };

        if (typeof ResizeObserver !== "undefined") {
            state.resizeObserver = new ResizeObserver(() => notify(dotNetRef));
            state.resizeObserver.observe(element);
        }

        if (typeof IntersectionObserver !== "undefined") {
            state.intersectionObserver = new IntersectionObserver((entries) => {
                entries.forEach((entry) => {
                    if (entry.isIntersecting) {
                        notify(dotNetRef);
                    }
                });
            });
            state.intersectionObserver.observe(element);
        }

        if (!visibilityListenerAdded) {
            document.addEventListener("visibilitychange", () => {
                if (document.hidden) {
                    return;
                }

                registry.forEach((entry) => notify(entry.dotNetRef));
            });
            visibilityListenerAdded = true;
        }

        registry.set(containerId, state);
        setTimeout(() => notify(dotNetRef), 0);
    }

    function unregister(containerId) {
        const existing = registry.get(containerId);
        if (!existing) {
            return;
        }

        if (existing.resizeObserver) {
            existing.resizeObserver.disconnect();
        }

        if (existing.intersectionObserver) {
            existing.intersectionObserver.disconnect();
        }

        registry.delete(containerId);
    }

    window.silkhatVisualHost = {register, unregister};
})();
