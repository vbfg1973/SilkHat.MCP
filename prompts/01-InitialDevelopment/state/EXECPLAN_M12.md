# M12: IDE menu wrapper + view switching

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This plan is maintained according to `PLANS.md` in the repository root.

## Purpose / Big Picture

Introduce a native MudBlazor menu wrapper for the IDE view to control visible panels. Replace direct buttons with menu actions, add tab navigation and toolbox toggling, and prepare for future views while keeping current functionality intact and fully tested.

## Progress

- [x] Draft menu structure and IDE view switching state/actions.
- [x] Implement menu UI wrapper with submenus and disabled items.
- [x] Replace Symbols button with menu action and wire view switching.
- [x] Implement "Tabs" view selection + open-tabs submenu switching.
- [x] Implement Toolbox toggle via menu (hide/show).
- [x] Add tests for menu behaviors and view switching.
- [x] Run `dotnet test`.

## Surprises & Discoveries

- MudMenu popovers are rendered by MudPopoverProvider outside the component subtree; tests now invoke menu action handlers directly via reflection to validate dispatch without brittle UI popover interaction.

## Decision Log

None yet.

## Outcomes & Retrospective

- **Delivered:** IDE menu wrapper with repository-grouped solution selection under File, fully componentized IDE layout, and view-host swap ready for future views.
- **Testing:** UI/state tests updated for new solution entry shape; menu UI test removed per request; full `dotnet test` passes.
- **Follow-up:** If menu behavior needs coverage again, consider handler-level tests to avoid MudPopover JS dependencies.

## Context and Orientation

IDE layout lives in `src/SilkHat.Ui/Pages/Ide.razor` and tabbed view in `src/SilkHat.Ui/Components/Ide/IdeTabs.razor`. Symbols popup currently opened from the IdeTabs toolbar. Toolbox visibility is currently a local toggle in IdeTabs; this must be lifted to a view-level state for menu control.

## Plan of Work

1) Add IDE view state (Tabs vs other views) and toolbox visibility state to Fluxor (new actions/reducers).
2) Build a MudBlazor menu bar wrapper in IDE view with top-level menus and submenu items. Ensure disabled items render as such.
3) Wire "Tabs" menu to focus tab view and open-tabs submenu to switch active tab.
4) Replace symbols toolbar button with menu item. Keep popup behavior unchanged.
5) Wire Toolbox menu toggle to state; update IdeTabs to observe state.
6) Add tests verifying menu items dispatch actions and view state changes (including toolbox toggle and tab switching).
7) Run `dotnet test`.

## Validation and Acceptance

- Menu bar displays top-level menus and submenus; disabled items are greyed out.
- Tabs menu returns to current tab view and lists open tabs; selecting a tab switches active tab.
- Symbols menu item opens the symbol popup.
- Toolbox menu toggle hides/shows toolbox and tab view expands appropriately.
- Tests cover menu dispatch and view switching, and all tests pass.

## Idempotence and Recovery

Menu actions should be safe to invoke repeatedly. If no tabs are open, Tabs submenu should still render without errors.

## Interfaces and Dependencies

- Fluxor state/actions for IDE view selection and toolbox visibility.
- MudBlazor menu components (MudMenu, MudMenuItem, etc).

## Test Plan

Update or add tests in `tests/SilkHat.Ui.Tests` to validate:
- Menu renders expected items and disabled items.
- Symbols menu dispatches action to open popup.
- Tabs submenu switches active tab.
- Toolbox toggle updates state and hides/shows toolbox panel.

Run `dotnet test` in repo root.
