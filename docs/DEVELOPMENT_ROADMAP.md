# Implementation roadmap

- [x] Inspect specification, authentication and tools; create new public repository.
- [x] M0: verify/push three-project solution, health checks and reproducible CI (10b78da; 2 HTTP tests passed locally).
- [x] M1: entities, SQL Server constraints/indexes, migration and model tests; actual SQL migration/constraint verification passed in CI.
- [x] M2: Identity, registration/login/profile, roles, CSRF and authorization tests (local isolated HTTP provider; SQL CI separately reports execution).
- [x] M3: organizations, project create/read/update/archive, membership, resource authorization.
- [x] M4: tasks, assignment, transitions, filters/pagination and Kanban; CI verified, including parallel SQL writes (3f4ce11).
- [x] M5: database dashboard, comments, activity, reports and safe CSV export.
- [x] M6: in-app notifications, idempotent deadlines and protected administration (SQL race checks run in CI).
- [x] M7: responsive UI/accessibility, development OpenAPI and Chromium SQL journey passed (e8229b9 onward).
- [x] M8: security/formatting/SQL regressions, actual screenshots and setup documentation; code checkpoint d015f5e passed CI and Security.

Each increment must build and pass its relevant tests before committing. Completion
boxes reflect verified functionality, not merely generated files. Local SQL Server
installation is not authorized; provider-specific verification uses isolated CI
SQL Server databases, and local database verification remains explicitly limited.
