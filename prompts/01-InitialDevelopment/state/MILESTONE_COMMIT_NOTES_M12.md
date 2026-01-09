M12: add IDE menu wrapper and repository-grouped solution selection

- add IDE layout state/actions to drive view selection and toolbox visibility
- replace header/selector layout with componentized IDE workspace (header, menu, main view host)
- move solution selection into File > Repositories submenu, grouped by repository
- introduce repository name on solution entries and update effects/tests accordingly
- align workspace/menu padding and fix file viewer scrolling via flex panel adjustments
- drop menu UI test per requirement; remaining UI/state tests updated and passing

Tests:
- dotnet test
