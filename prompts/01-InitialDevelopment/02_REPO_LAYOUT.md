# Repository Layout (Enforced)

Root:
- SilkHat.sln (solution in root)
- docker-compose.yml (root)
- PLANS.md
- .agents/

Folders:
- /src        => all implementation projects
- /tests      => all test projects
- /docs       => documentation (including architecture notes)
- /prompts    => prompt packs (this pack is prompts/01-InitialDevelopment)

Acceptance:
- Build scripts and docker assets must reference these paths.
