# Architecture and specification decisions

Primary source: ProjectFlow Solo Developer Implementation Blueprint v1.1, October
2026, supplied PDF. It has two pages, ending at the Scope and Priorities heading.
Its running header says Two-Developer, but the body and explicit user request say
solo developer. No detailed schema/API/roadmap was present; the supplied master
request fills those gaps. Organizations, Kanban and reporting/export come from
the PDF and are part of the implementation backlog.

- Core: entities, strongly typed statuses, DTOs, service contracts and domain rules.
- Infrastructure: SQL Server EF Core context, Identity storage and shared services.
- Web: MVC pages and API controllers in one process; resource access is checked in
  shared services, not delegated to button visibility or caller-supplied IDs.
- UnitTests: pure business rules. IntegrationTests: actual HTTP and persistence.

The single Web host avoids duplicated Identity configuration and business logic.
Controllers depend on shared services; Infrastructure depends on Core. Core does
not depend on MVC or the SQL provider. No external email or paid infrastructure.

Planned policy decisions: organization ownership is a resource-scoped permission;
registration never grants a global administrator role. Project managers operate
only within authorized organizations/projects. Administrators are bootstrapped
explicitly with secret local configuration, never automatic first-user elevation.
