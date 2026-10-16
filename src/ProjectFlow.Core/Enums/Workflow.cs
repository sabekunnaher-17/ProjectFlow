namespace ProjectFlow.Core.Enums;

public enum ProjectStatus { Planned, Active, OnHold, Completed, Archived }
public enum TaskStatus { Todo, InProgress, InReview, Completed, Cancelled }
public enum TaskPriority { Low, Medium, High, Critical }
public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Project Manager";
    public const string Member = "Team Member";
    public static readonly string[] All = [Admin, Manager, Member];
}
