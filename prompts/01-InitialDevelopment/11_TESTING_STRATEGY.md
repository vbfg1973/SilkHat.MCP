# Testing Strategy

This repository uses xUnit for all tests. Controller tests live in `tests/SilkHat.Api.Tests`, service tests in `tests/SilkHat.Tests`, and UI component tests in `tests/SilkHat.Ui.Tests` using bUnit.

## Controller tests

Controller tests must verify both dependency interactions and response payloads. When a controller calls into a store or service, tests must assert the call and inspect the resulting `ActionResult` (status code + body). For database-backed controllers, use EF Core InMemory with a SaveChanges interceptor to prove persistence calls occur.

## Service tests

Service unit tests should mock dependencies and verify interactions, responses, and edge cases. Integration tests should exercise the real implementation without infrastructure dependencies (use temp folders or in-memory storage where appropriate).

## UI component tests

UI tests use bUnit. Each page/component should have at least one test that verifies it renders expected content and that it handles its key interactions or service calls correctly. Register MudBlazor with `AddMudServices()`. For components that call APIs, use a fake `HttpMessageHandler` and inject `RepositoryApiClient` with a test `HttpClient`. For components that use `IJSRuntime` (e.g., `ThemeService`), use bUnit's `JSInterop` to register expected calls.
