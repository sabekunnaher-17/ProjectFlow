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

Upcoming: `/api/v1/organizations`, `/projects`, `/projects/{id}/members`, `/tasks`,
`/tasks/{id}/comments`, `/notifications`, `/dashboard`, `/reports`, `/admin/users`.

Cookie-authenticated mutations require an antiforgery header, including APIs.
Protected API requests return 401/403 rather than HTML login redirects. DTOs and
bounded pagination are required; data access always enforces resource scope.
Development Swagger/OpenAPI will document implemented endpoints, not invented
responses. Validation errors use ProblemDetails; internal exception text is hidden.
