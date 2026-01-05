# UI (Blazor WASM + MudBlazor)

Theme:
- Standard MudBlazor theme setup
- Light/dark toggle persists to local storage

Views:
1) Dashboard
- list/create/edit repository configs and groups
- load repo or group (shows streaming progress)

2) IDE-like
- Left: tree (solutions/projects/folders/files) with icons
- Center: tabbed file viewer (plain text first; highlighting best-effort later)
- Right: context panel (file history, selected symbol summary, etc.)

API access:
- Typed HttpClient preferred (NSwag optional later); MVP can use hand-rolled client.
- Streaming load consumes NDJSON and renders progress log.
