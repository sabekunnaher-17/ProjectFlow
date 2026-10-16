# Configuration, database safety and deployment

Follow the root README for clone/build/migration/run commands. SQL Server is the
production provider; no local SQL Server installation or migration was performed
on the development PC. The portable SDK installation changed no global PATH,
Windows service, registry, firewall or certificate trust settings.

| Setting | Purpose |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Development locally; Production for deployment |
| `ConnectionStrings__DefaultConnection` | Explicit dedicated SQL target, supplied securely |
| `Bootstrap__Email` | Existing account for the explicit first-Admin CLI; remove after use |
| `Deadlines__Enabled` | Defaults true; disable only if the operator runs scheduling elsewhere |
| `PROJECTFLOW_TEST_SQL_SERVER` | Test fixture server connection; creates new `_test` databases, never reuses names |
| `PROJECTFLOW_BROWSER_TESTS` | `1` opts into installed Chromium journey; CI supplies it |
| `PROJECTFLOW_SCREENSHOTS` | Optional test-only artifact output directory |

The example JSON contains placeholders and is not automatically read. The Web
project has a UserSecretsId for optional development storage; never use User Secrets
as a production vault. The EF factory intentionally reads process environment,
not Web User Secrets. Missing migration targets fail; `-- --design-only` is only
for offline model/script generation and always selects a reserved invalid host.

## SQL Server

Use SQL Server 2022+ with a dedicated new database. Review the generated migration
SQL before applying it. Use a separate schema operator for DDL; normal app access
needs scoped Identity/project data permissions and the built-in application-lock
procedure. Do not give the running application server-admin privileges. Back up
real data and review each future migration; this project never auto-migrates at boot.

CI's server-admin credential exists only inside a disposable runner so the fixture
can create uniquely named databases. It is generated and masked at runtime, never
committed. CI binds the server to loopback and accepts only that disposable server's
self-signed certificate. This changes no machine certificate trust. Database name
collisions abort instead of dropping or reusing data. Container removal cleans up
only CI-created resources; local services/databases are untouched.

## Production preparation

- Serve HTTPS and use Production mode. HSTS/Secure cookies are enabled there.
- Use a trusted SQL TLS certificate and least-privilege credentials from a secret store.
- Persist/protect ASP.NET Data Protection keys using the host's supported protected
  key store; protect cookie-key backups and do not commit them.
- Configure backups, retention, logs and monitoring with the deployment operator.
- Forwarded IP headers are not trusted. Behind a proxy, configure explicit trusted
  proxies and shared edge rate limits before relying on client-IP policies.
- The default per-instance account limit supplements Identity's database-backed lockout.
- No paid infrastructure, production database, email integration or deployment was created.
