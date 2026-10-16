using System.ComponentModel.DataAnnotations;

namespace ProjectFlow.Core.DTOs;

public sealed class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string DisplayName { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12), DataType(DataType.Password)] public string Password { get; set; } = "";
}

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(128), DataType(DataType.Password)] public string Password { get; set; } = "";
}

public sealed class ProfileRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string DisplayName { get; set; } = "";
}

public sealed record AccountDto(string Id, string DisplayName, string Email, IReadOnlyList<string> Roles);
