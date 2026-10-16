using System.ComponentModel.DataAnnotations;

namespace ProjectFlow.Core.DTOs;

public sealed class AdminUserRequest
{
    [Required, StringLength(128)] public string ConcurrencyStamp { get; set; } = "";
    [Required, StringLength(30)] public string Role { get; set; } = "";
    public bool IsActive
    {
        get; set;
    }
}
public sealed record AdminUserDto(string Id, string DisplayName, string Email, bool IsActive, string ConcurrencyStamp, IReadOnlyList<string> Roles);
public sealed record NotificationDto(Guid Id, Guid? TaskItemId, string Message, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt);
