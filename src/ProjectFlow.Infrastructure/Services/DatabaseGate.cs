using Microsoft.EntityFrameworkCore;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.Infrastructure.Services;

internal static class DatabaseGate
{
    // Call inside a transaction. SQL Server serializes cross-host administrative
    // and deadline sweeps without a new table or external lock dependency.
    public static Task LockAsync(ApplicationDbContext db, bool administration, CancellationToken ct)
    {
        if (!db.Database.IsSqlServer())
            return Task.CompletedTask;
        return administration ? db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=N'ProjectFlow.AdminChanges', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=5000; IF @r < 0 THROW 51000, N'Administrative operation is busy.', 1;", ct)
            : db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=N'ProjectFlow.Deadlines', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=5000; IF @r < 0 THROW 51000, N'Deadline sweep is busy.', 1;", ct);
    }
}
