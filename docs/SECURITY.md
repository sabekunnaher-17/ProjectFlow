# Security

Framework Identity password hashing, least-privilege roles, resource membership
checks, antiforgery validation for every cookie mutation, safe DTOs, encoded Razor
output, rate-limited authentication, secure production cookies and generic errors
are required implementation gates. At foundation, safe exception responses, CSP,
nosniff and referrer policy are present; Identity/resource checks remain pending.

No committed credentials, default administrator password, automatic role elevation,
or implicit database migration. Local User Secrets and process environment settings
are the configuration boundary. No untrusted forwarded IP headers are honored.

CI performs full-history Gitleaks scanning and NuGet transitive vulnerability checks.
Report defects privately through the repository owner's available contact channels;
do not publish real credentials in issues.
