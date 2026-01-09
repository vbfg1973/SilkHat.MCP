# M11: Harden visualization rendering for Mermaid and D3

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This plan is maintained according to `PLANS.md` in the repository root.

## Purpose / Big Picture

After this change, the IDE’s Mermaid (and future D3) visualizations will render reliably inside tabs and popups, even when panels are hidden, resized, or reopened. The UI will provide a shared rendering host with consistent safeguards and the ability to pass the current MudTheme color palette to visualization renderers for visual consistency. Users should be able to open the call stack Mermaid view and see a reliable diagram, and new visual components will inherit the same safety features without bespoke code.

We will also tighten IDE layout rules for the file viewer, toolbox, and repository tree, and ensure the theme toggle reliably applies dark/light mode and is test-covered.

## Progress

- [x] (2026-01-09 00:39Z) Drafted the visualization host design, JS interop contract, and palette model; identified call sites and tests to update.
- [x] (2026-01-09 00:39Z) Implemented the visualization host component/base class and JS observers for visibility/resize; wired Mermaid rendering through the new host and palette.
- [x] (2026-01-09 00:39Z) Added D3 asset and a minimal D3 viewer contract to prove the host works for a non-Mermaid renderer.
- [x] (2026-01-09 00:49Z) Updated tests for render/re-render and palette propagation; ran `dotnet test`.
- [x] (2026-01-09 01:08Z) Updated IDE layout for resizable file viewer/toolbox and tree sizing/scroll rules.
- [x] (2026-01-09 01:08Z) Fixed theme toggle to reliably apply dark mode; added tests for theme persistence.
- [x] (2026-01-09 01:19Z) Verified updated toolbox/tree layout wiring and re-ran `dotnet test`.
- [x] (2026-01-09 00:39Z) Updated `docs/architecture.md` to document the finalized rules, component responsibilities, and testing requirements.

## Surprises & Discoveries

None yet.

## Decision Log

None yet.

## Outcomes & Retrospective

- Tests pass (`dotnet test`) after adding visualization host coverage, IDE layout updates, and theme toggle persistence checks.
- Tests re-run after layout wiring changes; all green.

## Context and Orientation

Visualization rendering currently lives in `src/SilkHat.Ui/Components/Ide/IdeMermaidViewer.razor` and its JS helper `src/SilkHat.Ui/wwwroot/js/mermaid-renderer.js`. Mermaid is loaded from `src/SilkHat.Ui/wwwroot/js/mermaid.min.js` in `src/SilkHat.Ui/wwwroot/index.html`. The call stack popup uses Mermaid in `src/SilkHat.Ui/Components/Ide/IdeCallStackPopup.razor`. UI architecture and visualization rules are documented in `docs/architecture.md` under “Visualization Rendering (Mermaid + D3)”.

We need a shared visualization host that handles common pitfalls: rendering into hidden elements, resizes, and tab/popup visibility changes. The host will expose a JS interop contract that notifies Blazor when re-rendering is required. We will also support passing a MudTheme-derived palette into Mermaid/D3 renderers, so visual components can match the app theme.

## Plan of Work

First, define a reusable visualization host component or base class that can be used by Mermaid and future D3 components. This host will provide a stable container id, register with JS to observe visibility and size changes, and call back into Blazor when a re-render is required. The host must support teardown to avoid memory leaks.

Next, implement a JS helper (new file in `wwwroot/js`) that registers `ResizeObserver` and `IntersectionObserver` to detect when the container becomes visible or is resized. The helper should call back into Blazor via a `NotifyRenderRequested` method. It should also support unregistering.

Then, update `IdeMermaidViewer` to inherit from or use the host component. It should render on initial load, on parameter changes, and on render notifications. It should accept a theme palette object and pass it into `mermaid.initialize` as `themeVariables` before rendering. If a palette is not available, it should fall back to the Mermaid defaults.

Add D3 support by including the latest stable D3 build in `wwwroot/js` and referencing it in `index.html`. Create a minimal D3 viewer component (or a stub renderer) that uses the visualization host, so we can test the host with a non-Mermaid renderer. It can be a no-op render that records invocation until a real D3 visualization is added, but it must exercise the host/interop path.

Update tests to cover the new rendering host, including:

- registering with JS interop when the component renders,
- invoking render on initial load and when a diagram is provided,
- invoking render when the host signals a render request,
- passing palette data into the JS render call.

Update `docs/architecture.md` to reflect the new component, JS contract, and palette integration rules. Add explicit testing requirements for visibility/resize re-render behavior and palette propagation.

Add IDE layout rules for the file viewer/toolbox split (horizontal resize only), tree scaling/scrolling rules, and theme toggle test coverage for dark mode persistence.

## Concrete Steps

1) Add a visualization host component/base class and JS interop contract.

2) Add `wwwroot/js/visualization-host.js` and include it in `src/SilkHat.Ui/wwwroot/index.html` before Mermaid/D3 scripts.

3) Update `IdeMermaidViewer` to use the host contract and pass palette info.

4) Add `wwwroot/js/d3.min.js` (latest stable, locally served) and add a minimal D3 viewer component using the same host.

5) Update tests in `tests/SilkHat.Ui.Tests` to verify host registration, render invocation, and palette data propagation.

6) Update `docs/architecture.md` visualization section and UI architecture notes.

7) Run `dotnet test` from the repo root and capture results.
8) Update IDE layout and theme toggle behavior; add tests for theme persistence.
9) Run `dotnet test` again.

## Validation and Acceptance

- Run `dotnet test` from the repo root; expect all tests to pass.
- Open the call stack popup in the UI and switch between tabs or resize the popup; the Mermaid diagram should render consistently.
- When the UI theme is toggled between light/dark, the Mermaid output should adapt to the palette settings (colors change or are at least passed through to Mermaid theme variables).
- For the D3 host, a minimal component should register and invoke render without throwing, demonstrated via tests.

## Idempotence and Recovery

The visualization host and JS helpers should be safe to register/unregister repeatedly as components mount/unmount. If registration fails or observers are unavailable, fall back to a no-op and continue showing text content to avoid blank UI. D3/Mermaid assets are local; if updates fail, revert to prior assets and rerun tests.

## Artifacts and Notes

Any JS interop call expectations should be captured in test assertions. If new theme variables are added, document them in `docs/architecture.md` with a short example of the payload passed to JS.

## Interfaces and Dependencies

- JS helper: `window.silkhatVisualHost.register(containerId, dotNetRef)` and `window.silkhatVisualHost.unregister(containerId)` for observers. `NotifyRenderRequested` will be invoked on the `.NET` side.
- Mermaid renderer: `window.silkhatMermaid.render(containerId, diagram, theme)` where `theme` is optional and mapped to Mermaid theme variables.
- D3 renderer: `window.silkhatD3.render(containerId, data, theme)` stubbed for now but must be callable.
- Theme palette model: define a simple DTO in `src/SilkHat.Ui/Models` that captures the palette colors used by Mermaid/D3 (primary, secondary, surface, text).

## Test Plan

Add/update tests in `tests/SilkHat.Ui.Tests`:

- `IdeMermaidViewer` tests should assert `silkhatVisualHost.register` is called and `silkhatMermaid.render` is invoked with a non-empty diagram and palette.
- Add a test that simulates a render request by invoking the JS callback and verifying a second render call.
- Add tests for the new D3 viewer component to confirm it registers and attempts a render call.

Run `dotnet test` in `/home/vbfg/RiderProjects/SilkHat.MCP` and confirm no failures.
