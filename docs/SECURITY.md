# Security

Framework Identity password hashing, least-privilege roles, resource membership
checks, antiforgery validation for every cookie mutation, safe DTOs, encoded Razor
output, rate-limited authentication, secure production cookies and generic errors
are required implementation gates. Identity, CSRF on all MVC/API mutations, generic
login errors, default Team Member registration, CSP and safe errors are implemented.
Project/organization queries enforce membership/ownership scope in SQL before
materialization. A global Project Manager role permits creating projects only in
organizations they joined. Explicit project-manager assignment grants management
inside that project; it never grants access to unrelated organizations/projects.
Owners/Admin may manage organization membership and their contained projects.
Task checks accompany their later milestone.
Task reads require project scope; create/edit require project management, and status
changes require the assignee or management. Assignment requires active membership
and a composite database FK. All task/project writes use concurrency tokens; task
writes also advance the project token to serialize planning/archive changes.

Admin mutations take a transaction-owned SQL Server application lock, recheck the
actor's active Admin membership in SQL, reload the target and reject stale stamps.
Last-active-admin counting happens inside the same operation. Roles are never
accepted by registration/profile requests. Database owners/operators are trusted;
direct SQL modifications are outside application authorization guarantees.

Event notifications save atomically with task/comment changes. Deadline sweeps use
a separate database application lock and unique recipient/event keys. Notification
reads and read-markers require recipient identity plus current task scope. The
five-minute worker never exposes an unauthenticated mutation endpoint.

Account endpoints allow 20 submissions per client IP per minute per app instance,
with no queue. Identity locks an account for 15 minutes after five failed passwords.
Unknown/inactive accounts receive generic failures and normal Identity hash work.
No proxy/forwarded headers are trusted. Distributed deployments require a shared
edge rate limit and explicitly trusted proxy configuration. Cookies are HttpOnly,
SameSite Lax and Secure in production, expire after eight hours without sliding.
Security stamps and IsActive are checked on every authenticated request. Deactivation
and role/password changes must invalidate sessions. Production requires TLS and
protected persistent ASP.NET Data Protection key storage; don't commit key files.

Registration doesn't require email delivery; email confirmation/password reset are
not currently offered. Bootstrap is an explicit operator CLI for an existing user
and refuses when any administrator exists. No public endpoint grants Admin.

Framework guidance: [Identity configuration](https://learn.microsoft.com/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0),
[antiforgery](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0),
[rate limiting](https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-10.0).

No committed credentials, default administrator password, automatic role elevation,
or implicit database migration. Local User Secrets and process environment settings
are the configuration boundary. No untrusted forwarded IP headers are honored.

CI performs full-history Gitleaks scanning and NuGet transitive vulnerability checks.
Report defects privately through the repository owner's available contact channels;
do not publish real credentials in issues.
