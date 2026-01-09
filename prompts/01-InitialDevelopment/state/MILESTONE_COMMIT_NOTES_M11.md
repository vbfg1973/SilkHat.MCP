M11: harden visualization components + IDE layout/toolbox toggle + theme persistence

Summary:
- Added a resilient visualization host for Mermaid/D3 and updated architecture rules + tests for palette propagation.
- Reworked IDE layout: fixed-width tree/toolbox, flexible file viewer, uniform spacing, scroll behavior, and toolbox hide/show.
- Fixed theme toggle persistence (MudSwitch uses Value binding) and added UI test for localStorage persistence.

Key changes:
- Mermaid/D3 rendering host JS interop + base component; theme palette integration.
- IDE file viewer and toolbox separated into distinct panels; new `IdeToolbox` component; layout CSS tuned.
- Tree view scrolls both axes; file viewer scrolls both axes; whitespace preserved.

Tests:
- dotnet test
