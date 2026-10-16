using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Entities;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.IntegrationTests;

public sealed class SchemaTests
{
    [Fact]
    public void SqlServerSchemaHasAssignmentMembershipAndDateConstraintsWithoutConnecting()
    {
        using var db = new DesignTimeFactory().CreateDbContext([]);
        var sql = db.Database.GenerateCreateScript();
        Assert.Contains("FK_Tasks_ProjectMembers_ProjectId_AssigneeId", sql);
        Assert.Contains("CK_Task_Dates", sql);
        Assert.Contains("CK_Project_Status", sql);
        Assert.Contains("IX_Notifications_UserId_EventKey", sql);
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Fact]
    public void EditableEntitiesHaveConcurrencyTokensAndSqlSafeMembershipKeys()
    {
        using var db = new DesignTimeFactory().CreateDbContext([]);
        Assert.True(db.Model.FindEntityType(typeof(TaskItem))!.FindProperty(nameof(Entity.Version))!.IsConcurrencyToken);
        Assert.Equal(128, db.Model.FindEntityType(typeof(ProjectMember))!.FindProperty(nameof(ProjectMember.UserId))!.GetMaxLength());
        Assert.Equal(128, db.Model.FindEntityType(typeof(Microsoft.AspNetCore.Identity.IdentityUserLogin<string>))!.FindProperty("ProviderKey")!.GetMaxLength());
        Assert.All(db.Model.FindEntityType(typeof(TaskItem))!.GetForeignKeys(), x => Assert.Equal(DeleteBehavior.Restrict, x.DeleteBehavior));
    }
}
