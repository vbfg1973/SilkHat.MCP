(() => {
  function render(containerId, data, theme) {
    const container = document.getElementById(containerId);
    if (!container) {
      return;
    }

    const label = data && data.label ? data.label : "D3 renderer placeholder";
    const color = theme && theme.primary ? theme.primary : "#222";

    container.innerHTML = "";
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.setAttribute("viewBox", "0 0 300 80");
    svg.setAttribute("width", "100%");
    svg.setAttribute("height", "80");

    const text = document.createElementNS("http://www.w3.org/2000/svg", "text");
    text.setAttribute("x", "16");
    text.setAttribute("y", "40");
    text.setAttribute("fill", color);
    text.setAttribute("font-size", "14");
    text.textContent = label;

    svg.appendChild(text);
    container.appendChild(svg);
  }

  function clear(containerId) {
    const container = document.getElementById(containerId);
    if (container) {
      container.innerHTML = "";
    }
  }

  window.silkhatD3 = { render, clear };
})();
