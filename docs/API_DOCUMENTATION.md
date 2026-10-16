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

Task endpoints implemented and verified in SQL Server CI:

| Method | Route under `/api/v1` | Behavior |
| --- | --- | --- |
| GET/POST | `/tasks` | Scoped paginated list / management creates Todo task |
| GET/PUT | `/tasks/{id}` | Scoped detail / management edits active task with Version |
| PUT | `/tasks/{id}/status` | Assignee/management, Status + Version, allowed transition |

Task filters: Keyword, ProjectId, AssigneeId, Status, Priority, From/To (due-date
range), Overdue, Mine, Sort (due/priority/title), Page and PageSize. Limits match
projects. Terminal tasks must be reopened by management before editing. A completed
project must be reopened before task changes; archived projects remain read-only.
Planning dates use the UTC calendar; event timestamps are UTC offsets. Project/task
mutation concurrency protects date/membership/archiving checks against stale edits.

Insight endpoints:

| Method | Route under `/api/v1` | Behavior |
| --- | --- | --- |
| GET | `/dashboard` | Authorized project/task totals, statuses, open deadlines and activity |
| GET | `/activity?projectId=&page=1` | Scoped activity, 20 per page |
| GET/POST | `/tasks/{taskId}/comments` | Scoped history, 20 per page / Body up to 3,000 chars |
| GET | `/reports/tasks.csv` | TaskQuery filters, maximum 5,000 rows, formula-safe cells |

Comments are append-only and archived projects are read-only. CSV export rejects
oversized reports rather than truncating. Date ranges apply to due dates; all
reports honor the same scope as task lists. Upcoming deadlines exclude terminal
tasks before pagination.

| Method | Route under `/api/v1` | Behavior |
| --- | --- | --- |
| GET | `/notifications?page=1&unread=false` | Current user's accessible notifications, 20 per page |
| PUT | `/notifications/{id}/read` | Private, idempotent read marker; CSRF required |
| GET | `/admin/users?keyword=&page=1` | Admin-only bounded user list |
| PUT | `/admin/users/{id}` | Admin-only Role/IsActive/ConcurrencyStamp mutation |

Admin edits replace global roles with one supported role; they invalidate the user's
security stamp. Stale edits and last-active-admin removal return 409. Administrators
and their current activation are rechecked inside the serialized database operation.

Cookie-authenticated mutations require an antiforgery header, including APIs.
Protected API requests return 401/403 rather than HTML login redirects. DTOs and
bounded pagination are required; data access always enforces resource scope.
Development `/openapi/v1.json` documents implemented JSON endpoints and DTOs. It is
not mapped in production. The authenticated API uses same-origin Identity cookies;
obtain the CSRF token as described above for mutations. No bearer-token flow or
cross-origin access is offered. Validation errors use ProblemDetails; internal
exception text is hidden. MVC controllers are excluded from the API document.
