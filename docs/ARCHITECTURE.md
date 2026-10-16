# Architecture and specification decisions

Primary source: ProjectFlow Solo Developer Implementation Blueprint v1.1, October
2026, supplied PDF. It has two pages, ending at the Scope and Priorities heading.
Its running header says Two-Developer, but the body and explicit user request say
solo developer. No detailed schema/API/roadmap was present; the supplied master
request fills those gaps. Organizations, Kanban and reporting/export come from
the PDF and are implemented modules.

- Core: entities, strongly typed statuses, DTOs, service contracts and domain rules.
- Infrastructure: SQL Server EF Core context, Identity storage and shared services.
- Web: MVC pages and API controllers in one process; resource access is checked in
  shared services, not delegated to button visibility or caller-supplied IDs.
- UnitTests: pure business rules. IntegrationTests: actual HTTP and persistence.

The single Web host avoids duplicated Identity configuration and business logic.
Controllers depend on shared services; Infrastructure depends on Core. Core does
not depend on MVC or the SQL provider. No external email or paid infrastructure.

Policy decisions: organization ownership is a resource-scoped permission;
registration never grants a global administrator role. Project managers operate
only within authorized organizations/projects. Administrators are bootstrapped
explicitly with secret local configuration, never automatic first-user elevation.

Task services serialize against project changes using the project concurrency token.
Comments are append-only; completed tasks require management reopening for metadata
edits; completed projects require reopening for task changes. Archived projects are
read-only. Project membership cannot be removed while any task references it.

NotificationEvents adds actual assignment/status/comment events in the mutation's
transaction. DeadlineWorker calls DeadlineService every five minutes (first run after
five minutes); each due date receives at most one upcoming and one overdue notice
per recipient. `Deadlines__Enabled=false` disables only the scheduler for operators
running it elsewhere; no external email service is required. A database mutex and
unique event index prevent duplicates across hosts. Admin updates have a distinct
database mutex for safe last-admin checks. No extra schema/dependency is required.
