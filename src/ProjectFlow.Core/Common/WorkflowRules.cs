using ProjectFlow.Core.Enums;
using WorkStatus = ProjectFlow.Core.Enums.TaskStatus;

namespace ProjectFlow.Core.Common;

public static class WorkflowRules
{
    public static void Dates(DateOnly start, DateOnly end)
    {
        if (start == default || end == default || end < start)
            throw new DomainException("Provide valid dates; the end date must not precede the start date.");
    }

    public static void TaskDates(DateOnly start, DateOnly due, DateOnly projectStart, DateOnly projectEnd)
    {
        Dates(start, due);
        if (start < projectStart || due > projectEnd)
            throw new DomainException("Task dates must fall within the project's planning dates.");
    }

    public static void Transition(WorkStatus from, WorkStatus to, bool canManage)
    {
        if (!Enum.IsDefined(from) || !Enum.IsDefined(to)) throw new DomainException("Invalid task status.");
        if (from == to) return;
        var allowed = from switch
        {
            WorkStatus.Todo => to is WorkStatus.InProgress || (canManage && to == WorkStatus.Cancelled),
            WorkStatus.InProgress => to is WorkStatus.Todo or WorkStatus.InReview || (canManage && to == WorkStatus.Cancelled),
            WorkStatus.InReview => to is WorkStatus.InProgress or WorkStatus.Completed || (canManage && to == WorkStatus.Cancelled),
            WorkStatus.Completed => canManage && to == WorkStatus.InProgress,
            WorkStatus.Cancelled => canManage && to == WorkStatus.Todo,
            _ => false
        };
        if (!allowed) throw new DomainException("This task status transition is not allowed.");
    }

    public static int Progress(IEnumerable<WorkStatus> statuses)
    {
        var active = statuses.Where(x => x != WorkStatus.Cancelled).ToArray();
        return active.Length == 0 ? 0 : (int)Math.Round(100d * active.Count(x => x == WorkStatus.Completed) / active.Length);
    }

    public static void Editable(ProjectStatus status)
    {
        if (status == ProjectStatus.Archived) throw new DomainException("Archived projects are read-only.", FailureKind.Conflict);
    }

    public static void Version(Guid expected, Guid actual)
    {
        if (expected == default || expected != actual)
            throw new DomainException("This item changed. Reload it before saving again.", FailureKind.Conflict);
    }
}
