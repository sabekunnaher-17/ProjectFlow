using ProjectFlow.Core.Common;
using ProjectFlow.Core.Enums;
using WorkStatus = ProjectFlow.Core.Enums.TaskStatus;

namespace ProjectFlow.UnitTests;

public sealed class WorkflowRulesTests
{
    [Fact]
    public void ProjectDatesRejectReversal() =>
        Assert.Throws<DomainException>(() => WorkflowRules.Dates(new(2026, 10, 2), new(2026, 10, 1)));
    [Fact]
    public void TaskDatesRejectOutsideProject() =>
        Assert.Throws<DomainException>(() => WorkflowRules.TaskDates(new(2026, 10, 1), new(2026, 11, 1), new(2026, 10, 1), new(2026, 10, 31)));
    [Theory]
    [InlineData(WorkStatus.Todo, WorkStatus.InProgress)]
    [InlineData(WorkStatus.InProgress, WorkStatus.InReview)]
    [InlineData(WorkStatus.InReview, WorkStatus.Completed)]
    public void MemberCanAdvanceAssignedWork(WorkStatus from, WorkStatus to) => WorkflowRules.Transition(from, to, false);
    [Theory]
    [InlineData(WorkStatus.Todo, WorkStatus.Completed)]
    [InlineData(WorkStatus.Completed, WorkStatus.InProgress)]
    [InlineData(WorkStatus.InProgress, WorkStatus.Cancelled)]
    [InlineData(WorkStatus.Cancelled, WorkStatus.Todo)]
    public void MemberCannotBypassWorkflow(WorkStatus from, WorkStatus to) =>
        Assert.Throws<DomainException>(() => WorkflowRules.Transition(from, to, false));
    [Fact] public void ManagerCanReopenCompletedWork() => WorkflowRules.Transition(WorkStatus.Completed, WorkStatus.InProgress, true);
    [Fact] public void InvalidEnumsAreRejected() => Assert.Throws<DomainException>(() => WorkflowRules.Transition((WorkStatus)99, WorkStatus.Completed, true));
    [Fact] public void CancelledTasksDoNotInflateProgress() => Assert.Equal(50, WorkflowRules.Progress([WorkStatus.Completed, WorkStatus.Todo, WorkStatus.Cancelled]));
    [Fact]
    public void EmptyAndCancelledProjectsHaveZeroProgress()
    {
        Assert.Equal(0, WorkflowRules.Progress([]));
        Assert.Equal(0, WorkflowRules.Progress([WorkStatus.Cancelled]));
    }
    [Fact] public void StaleUpdatesAreRejected() => Assert.Throws<DomainException>(() => WorkflowRules.Version(Guid.NewGuid(), Guid.NewGuid()));
    [Fact] public void ArchivedProjectsRejectChanges() => Assert.Throws<DomainException>(() => WorkflowRules.Editable(ProjectStatus.Archived));
}
