# DocId Symbol Endpoints Snapshot

This file is a quick snapshot of the DocID-related symbol endpoints and their response DTO shapes so you can scan them in one place.

## New controllers/endpoints (added in M13)

Controller: `src/SilkHat.Api/Controllers/CodeSymbolDescriptionsController.cs`

Base route:
- `api/repositories/{id}/code/solutions/{solutionId}/symbols`

Endpoints:
- `GET /types/{docId}` -> `NamedTypeDescriptionDto`
- `GET /methods/{docId}` -> `MethodDescriptionDto`
- `GET /properties/{docId}` -> `PropertyDescriptionDto`
- `GET /fields/{docId}` -> `FieldDescriptionDto`
- `GET /events/{docId}` -> `EventDescriptionDto`
- `GET /namespaces/{docId}` -> `NamespaceDescriptionDto`

Notes:
- `{docId}` must be URL-encoded.
- All endpoints return `ProblemDetails` with category `Code` when not found/unsupported.

## Already existed (not created in M13)

- `src/SilkHat.Api/Controllers/CodeSymbolsController.cs`
  - `POST /api/repositories/{id}/code/solutions/{solutionId}/symbols/lookup`
  - Request: `SymbolLookupRequest` (DocumentationId + SymbolKey)
  - Response: `SymbolLookupResultDto`

- `src/SilkHat.Api/Controllers/MethodCallStackController.cs`
  - `POST /api/repositories/{id}/code/solutions/{solutionId}/methods/call-stack`
  - `POST /api/repositories/{id}/code/solutions/{solutionId}/methods/call-stack/mermaid`

## Response DTO shapes (current)

Supporting types:
- `SymbolModifiersDto`
  - `IsAbstract`, `IsSealed`, `IsStatic`, `IsVirtual`, `IsOverride`, `IsAsync`, `IsExtern`, `IsReadOnly`, `IsConst`, `IsPartial`
- `SymbolParameterDto`
  - `Name`, `TypeName`, `RefKind`, `IsOptional`
- `CodeLocationDto`
  - `Path`, `Span` (`CodeTextSpanDto`), `LineSpan` (`CodeLineSpanDto`)

`NamedTypeDescriptionDto`
- `DocumentationId`
- `Name`
- `Namespace`
- `FullName`
- `TypeKind` (Class/Struct/Interface/Enum/Delegate/Record/Unknown)
- `Accessibility`
- `IsGeneric`
- `TypeParameters` (string list)
- `Modifiers` (`SymbolModifiersDto`)
- `Location` (`CodeLocationDto` or null)

`MethodDescriptionDto`
- `DocumentationId`
- `Name`
- `ContainingType`
- `ContainingNamespace`
- `FullyQualifiedName`
- `ReturnType`
- `Accessibility`
- `Modifiers` (`SymbolModifiersDto`)
- `Parameters` (`SymbolParameterDto` list)
- `TypeParameters` (string list)
- `Location` (`CodeLocationDto` or null)

`PropertyDescriptionDto`
- `DocumentationId`
- `Name`
- `ContainingType`
- `ContainingNamespace`
- `FullyQualifiedName`
- `PropertyType`
- `Accessibility`
- `HasGetter`
- `HasSetter`
- `IsIndexer`
- `Modifiers` (`SymbolModifiersDto`)
- `Location` (`CodeLocationDto` or null)

`FieldDescriptionDto`
- `DocumentationId`
- `Name`
- `ContainingType`
- `ContainingNamespace`
- `FullyQualifiedName`
- `FieldType`
- `Accessibility`
- `Modifiers` (`SymbolModifiersDto`)
- `Location` (`CodeLocationDto` or null)

`EventDescriptionDto`
- `DocumentationId`
- `Name`
- `ContainingType`
- `ContainingNamespace`
- `FullyQualifiedName`
- `EventType`
- `Accessibility`
- `Modifiers` (`SymbolModifiersDto`)
- `Location` (`CodeLocationDto` or null)

`NamespaceDescriptionDto`
- `DocumentationId`
- `Name`
- `FullName`
- `IsGlobalNamespace`
- `Location` (`CodeLocationDto` or null)
