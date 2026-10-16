# ProjectFlow

A project and task management application delivered incrementally by Sabekunnaher Antora.

## Current state

Milestone 0: layered .NET 10 MVC foundation, liveness endpoint, security headers,
automated host checks, reproducible dependency locks and CI. Authentication and
database-backed features are under implementation; the landing page is not a
claim that those features are complete. See [the roadmap](docs/DEVELOPMENT_ROADMAP.md).

## Stack and architecture

C#, ASP.NET Core MVC and Web API in one host, Razor/Bootstrap 5/vanilla JavaScript,
ASP.NET Core Identity, EF Core and SQL Server. Web calls shared Infrastructure
services implementing Core contracts. APIs use `/api/v1`; no separate API process
or cross-origin cookie sharing is required.

## Setup

Install a supported .NET 10 SDK (pinned in global.json). SQL Server/LocalDB is
required for persistent features; no SQL Server engine is installed by this repo.

```powershell
git clone https://github.com/sabekunnaher-17/ProjectFlow.git
cd ProjectFlow
dotnet restore --locked-mode
dotnet build --no-restore
dotnet test tests/ProjectFlow.IntegrationTests --no-build
dotnet run --project src/ProjectFlow.Web --urls http://localhost:5080
```

Use User Secrets or process environment configuration for credentials; never put
passwords into tracked configuration. Database setup/migration commands will be
added when their implementation is verified. No database has been created or
migrated locally. HTTP above is local development only; production requires TLS.

Documentation: [architecture](docs/ARCHITECTURE.md), [schema](docs/DATABASE_SCHEMA.md),
[API](docs/API_DOCUMENTATION.md), [testing](docs/TESTING.md), [security](docs/SECURITY.md).
Screenshots will be added only after rendering the actual working UI.
