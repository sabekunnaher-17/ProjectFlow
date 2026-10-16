# Database schema plan

SQL Server with EF Core migrations; no automatic destructive recreation.

Identity users/roles and their framework relationship tables; Organizations and
OrganizationMembers (PDF organizational scope); Projects and ProjectMembers;
TaskItems; TaskComments; ActivityLogs; Notifications. User references use Identity
IDs. Project/task/organization keys use GUIDs. Membership pairs must be unique.

Assumptions to enforce during M1: restrictive delete behavior, server-maintained
UTC timestamps, DateOnly planning dates, bounded strings, indexes on scope/status/
assignee/deadlines, and concurrency tokens. Project progress derives from tasks;
no hardcoded dashboard totals. Detailed mappings and a diagram will accompany the
actual migration. No database or schema has been created at the foundation stage.
