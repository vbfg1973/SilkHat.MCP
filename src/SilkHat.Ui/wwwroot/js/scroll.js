window.silkhatScrollToHighlight = function () {
    try {
        const container = document.querySelector('.ide-file-scroll.active');
        if (!container) return;

        const target = container.querySelector('.ide-line-highlight');
        if (!target) return;

        target.scrollIntoView({ block: 'start', behavior: 'auto' });
        container.scrollTop = Math.max(0, container.scrollTop - 20);
    } catch (e) {
        // swallow
    }
};
