# Testing

```powershell
dotnet restore --locked-mode
dotnet build --no-restore
dotnet test --no-build
```

Foundation HTTP checks run in the real ASP.NET test host and do not connect to a
database. Domain tests cover dates, transitions, progress and concurrency rules.
Schema tests generate actual SQL Server DDL without connecting; they verify
constraints and key bounds, not migration execution or persistence. NuGet audits are enabled
for all transitive dependencies; vulnerability warnings fail restore.

SQL Server is unavailable locally. Never use a production or existing database for
tests. Provider-specific CI tests must create uniquely named empty `_test` databases
and clean up only resources created by that test run. A SQLite test fixture, if
used for local HTTP checks, cannot establish SQL Server migration compatibility.
