# Implementation roadmap

- [x] Inspect specification, authentication and tools; create new public repository.
- [x] M0: verify/push three-project solution, health checks and reproducible CI (10b78da; 2 HTTP tests passed locally).
- [x] M1: entities, SQL Server constraints/indexes, generated migration, domain/model tests. SQL execution pending.
- [ ] M2: Identity, registration/login/profile, roles, CSRF and authorization tests.
- [ ] M3: organizations, project CRUD, membership, resource authorization.
- [ ] M4: tasks, assignment, transitions, filters/pagination and Kanban.
- [ ] M5: database dashboard, comments, activity, reports and safe CSV export.
- [ ] M6: in-app notifications, deadlines and protected administration.
- [ ] M7: responsive UI/accessibility and development OpenAPI.
- [ ] M8: complete automated/security verification and setup documentation.

Each increment must build and pass its relevant tests before committing. Completion
boxes reflect verified functionality, not merely generated files. Local SQL Server
installation is not authorized; provider-specific verification will use isolated CI
SQL Server databases, and local database verification remains explicitly limited.
