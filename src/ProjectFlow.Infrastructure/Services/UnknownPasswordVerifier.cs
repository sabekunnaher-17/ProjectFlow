using Microsoft.AspNetCore.Identity;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.Infrastructure.Services;

// Apply Identity's normal hash work for absent/inactive accounts as well.
public sealed class UnknownPasswordVerifier
{
    private readonly PasswordHasher<ApplicationUser> hasher = new();
    private readonly ApplicationUser dummy = new();
    private readonly string hash;
    public UnknownPasswordVerifier() => hash = hasher.HashPassword(dummy, Guid.NewGuid().ToString("N"));
    public void Verify(string password) => hasher.VerifyHashedPassword(dummy, hash, password);
}
