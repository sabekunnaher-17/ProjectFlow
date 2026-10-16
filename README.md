# ProjectFlow

A project and task management application delivered incrementally by Sabekunnaher Antora.

## Current state

Milestones 0–1: layered .NET 10 MVC foundation, liveness endpoint, security headers,
domain rules, SQL Server model and initial migration, automated domain/host/schema
checks, reproducible dependency locks and CI. Authentication and
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
dotnet test --no-build
dotnet tool restore
dotnet run --project src/ProjectFlow.Web --urls http://localhost:5080
```

Use User Secrets or process environment configuration for credentials; never put
passwords into tracked configuration. For a **new, dedicated development database**,
set `ConnectionStrings__DefaultConnection` in the current process to your approved
SQL Server connection, then run:

```powershell
dotnet tool run dotnet-ef database update --project src/ProjectFlow.Infrastructure --startup-project src/ProjectFlow.Infrastructure
```

Review the target before applying; never target an existing or production database.
The factory's LocalDB fallback is for generating migrations only. No database has been created or
migrated locally. HTTP above is local development only; production requires TLS.

Documentation: [architecture](docs/ARCHITECTURE.md), [schema](docs/DATABASE_SCHEMA.md),
[API](docs/API_DOCUMENTATION.md), [testing](docs/TESTING.md), [security](docs/SECURITY.md).
Screenshots will be added only after rendering the actual working UI.
