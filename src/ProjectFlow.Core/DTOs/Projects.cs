using System.ComponentModel.DataAnnotations;
using ProjectFlow.Core.Enums;

namespace ProjectFlow.Core.DTOs;

public sealed class OrganizationRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public string Name { get; set; } = "";
}
public sealed class OrganizationMemberRequest
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
}
public sealed record MemberDto(string Id, string DisplayName, string Email);
public sealed record OrganizationDto(Guid Id, string Name, string OwnerId, bool CanManage, IReadOnlyList<MemberDto> Members);
public sealed class ProjectRequest
{
    [Required] public Guid OrganizationId { get; set; }
    [Required, StringLength(120, MinimumLength = 2)] public string Name { get; set; } = "";
    [StringLength(3000)] public string? Description { get; set; }
    [Required, StringLength(128)] public string ManagerId { get; set; } = "";
    [DataType(DataType.Date)] public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    [DataType(DataType.Date)] public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
    [EnumDataType(typeof(ProjectStatus))] public ProjectStatus Status { get; set; }
    public Guid Version { get; set; }
}
public sealed class MemberRequest
{
    [Required, StringLength(128)] public string UserId { get; set; } = "";
    public Guid Version { get; set; }
}
public sealed class VersionRequest { public Guid Version { get; set; } }
public sealed class ProjectQuery
{
    [StringLength(120)] public string? Keyword { get; set; }
    [EnumDataType(typeof(ProjectStatus))] public ProjectStatus? Status { get; set; }
    public Guid? OrganizationId { get; set; }
    [Range(1, 1000000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 12;
}
public sealed record PageDto<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int Pages => (int)Math.Ceiling((double)Total / PageSize);
}
public sealed record ProjectDto(Guid Id, Guid OrganizationId, string OrganizationName, string Name, string Description,
    string ManagerId, string ManagerName, DateOnly StartDate, DateOnly EndDate, ProjectStatus Status, Guid Version,
    int Progress, bool CanManage, IReadOnlyList<MemberDto> Members);
