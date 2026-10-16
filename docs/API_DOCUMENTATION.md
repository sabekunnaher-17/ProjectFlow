# API contract plan

Versioned same-origin JSON routes: `/api/v1/auth`, `/organizations`, `/projects`,
`/projects/{id}/members`, `/tasks`, `/tasks/{id}/comments`, `/notifications`,
`/dashboard`, `/reports`, `/admin/users`. At M0 only `/health/live` is implemented.

Cookie-authenticated mutations will require an antiforgery header, including APIs.
Protected API requests return 401/403 rather than HTML login redirects. DTOs and
bounded pagination are required; data access always enforces resource scope.
Development Swagger/OpenAPI will document implemented endpoints, not invented
responses. Validation errors use ProblemDetails; internal exception text is hidden.
