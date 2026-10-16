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

The current HTTP account tests use a fresh in-memory SQLite connection per test;
they cover real Identity hashing/cookies, registration role, encoded profile values,
CSRF rejection, logout, lockout, inactive-session rejection and IP rate limits.
Setting `PROJECTFLOW_TEST_SQL_SERVER` makes the same fixture create a unique
`ProjectFlow_<guid>_test` database, refuse any name collision, and apply migrations
before writes. It never reuses or drops a database. CI uses this mode in a dedicated
job with a disposable SQL Server 2022 container and generated, masked credentials.
The container is removed after the job; it is unrelated to developer data/services.
Local SQL tests remain unrun until a SQL Server instance is explicitly approved.

M2's SQL Server CI job passed against newly created databases with the actual
migration. M3 adds organization isolation, member/owner permissions, membership
mutations, stale versions, archive protection, rendered form actions/IDs/values
and validation preservation. Test counts and CI results must be read per commit.
