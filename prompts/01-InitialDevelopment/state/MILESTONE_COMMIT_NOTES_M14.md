M14: add DocID complexity strategies and endpoints

- add complexity DTOs/enums and analysis models for method/type complexity results
- implement cognitive, cyclomatic, and indentation strategies with a strategy factory
- add method and type complexity services and API endpoints (methods/complexity, named-types/complexity)
- wire DI registrations for complexity services/strategies
- add unit tests for strategies, factory, services, and API controllers
- classify record structs as Struct in analysis outputs and align sample tests accordingly

Tests:
- dotnet test
- REPO_ROOT=/home/vbfg/repos/c/dev/repos/ docker-compose up --build -d
- REPO_ROOT=/home/vbfg/repos/c/dev/repos/ docker-compose down
