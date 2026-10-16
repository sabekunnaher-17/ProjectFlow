# API contracts

Implemented same-origin JSON routes, in addition to `/health/live`:

| Method | Route | Behavior |
| --- | --- | --- |
| GET | `/api/v1/account/csrf` | Public, no-store token and HttpOnly antiforgery cookie |
| POST | `/api/v1/account/register` | DisplayName/Email/Password; 201, default Team Member |
| POST | `/api/v1/account/login` | Email/Password; cookie or generic 401 |
| POST | `/api/v1/account/logout` | Authenticated; 204 |
| GET | `/api/v1/account/me` | Current safe account DTO; 401 anonymous |
| PUT | `/api/v1/account/profile` | DisplayName only; authenticated; 204 |

Fetch a CSRF token and send its value as `X-CSRF-TOKEN` for every unsafe request,
including registration/login/logout. Fetch a new token after login/logout because
tokens bind to the current identity. Send same-origin cookies. Never persist a
password/token in browser localStorage. MVC forms include the equivalent hidden token.

Workspace/project endpoints now implemented under `/api/v1`:

| Method | Route | Behavior |
| --- | --- | --- |
| GET/POST | `/organizations` | Scoped list (up to 100) / create owned workspace |
| GET | `/organizations/{id}` | Scoped members/details |
| POST | `/organizations/{id}/members` | Owner/Admin adds registered email |
| GET/POST | `/projects` | Paginated scope-filtered list / create |
| GET/PUT | `/projects/{id}` | Scoped detail / manager/owner/Admin edits with Version |
| POST | `/projects/{id}/archive` | Version required; makes read-only |
| POST | `/projects/{id}/members` | UserId + Version; active organization member |
| DELETE | `/projects/{id}/members/{userId}` | Version body; manager cannot be removed |

Project filters: Keyword, Status, OrganizationId, Page (1–1,000,000), PageSize (1–100).
Lists return Items/Total/Page/PageSize/Pages. Projects include a Version GUID; send
the latest Version on mutations or receive 409. Cross-scope IDs return 404, visible
resources without mutation permission return 403. Project organization is immutable.

Upcoming: `/api/v1/tasks`, `/tasks/{id}/comments`, `/notifications`, `/dashboard`,
`/reports`, `/admin/users`.

Cookie-authenticated mutations require an antiforgery header, including APIs.
Protected API requests return 401/403 rather than HTML login redirects. DTOs and
bounded pagination are required; data access always enforces resource scope.
Development Swagger/OpenAPI will document implemented endpoints, not invented
responses. Validation errors use ProblemDetails; internal exception text is hidden.
