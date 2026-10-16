using Microsoft.AspNetCore.Identity;

namespace ProjectFlow.Infrastructure.Data;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
