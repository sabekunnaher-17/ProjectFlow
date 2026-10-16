# Testing

```powershell
dotnet restore --locked-mode
dotnet build --no-restore
dotnet test tests/ProjectFlow.IntegrationTests --no-build
```

Foundation HTTP checks run in the real ASP.NET test host and do not connect to a
database. Domain and persistence suites will be added with their features. Unit
test scaffolding is not counted as executed coverage. NuGet audits are enabled
for all transitive dependencies; vulnerability warnings fail restore.

SQL Server is unavailable locally. Never use a production or existing database for
tests. Provider-specific CI tests must create uniquely named empty `_test` databases
and clean up only resources created by that test run. A SQLite test fixture, if
used for local HTTP checks, cannot establish SQL Server migration compatibility.
