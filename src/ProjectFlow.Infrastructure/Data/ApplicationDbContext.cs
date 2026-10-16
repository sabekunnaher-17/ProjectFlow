using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Entities;

namespace ProjectFlow.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, TimeProvider? clock = null)
    : IdentityDbContext<ApplicationUser>(options)
{
    private readonly TimeProvider clock = clock ?? TimeProvider.System;
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskComment> Comments => Set<TaskComment>();
    public DbSet<ActivityLog> Activities => Set<ActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<ApplicationUser>().Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        b.Entity<ApplicationUser>().Property(x => x.Id).HasMaxLength(128);
        b.Entity<IdentityRole>().Property(x => x.Id).HasMaxLength(128);
        b.Entity<IdentityUserLogin<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        b.Entity<IdentityUserLogin<string>>().Property(x => x.ProviderKey).HasMaxLength(128);
        b.Entity<IdentityUserToken<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        b.Entity<IdentityUserToken<string>>().Property(x => x.Name).HasMaxLength(128);

        b.Entity<Organization>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.OwnerId);
        });
        b.Entity<OrganizationMember>(e =>
        {
            e.HasKey(x => new { x.OrganizationId, x.UserId });
            e.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.UserId);
        });
        b.Entity<Project>(e =>
        {
            e.ToTable("Projects", t =>
            {
                t.HasCheckConstraint("CK_Project_Dates", "[EndDate] >= [StartDate]");
                t.HasCheckConstraint("CK_Project_Status", "[Status] BETWEEN 0 AND 4");
            });
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Description).HasMaxLength(3000);
            e.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.OrganizationId, x.Status });
            e.HasIndex(x => x.ManagerId);
        });
        b.Entity<ProjectMember>(e =>
        {
            e.HasKey(x => new { x.ProjectId, x.UserId });
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.UserId);
        });
        b.Entity<TaskItem>(e =>
        {
            e.ToTable("Tasks", t =>
            {
                t.HasCheckConstraint("CK_Task_Dates", "[DueDate] >= [StartDate]");
                t.HasCheckConstraint("CK_Task_Status", "[Status] BETWEEN 0 AND 4");
                t.HasCheckConstraint("CK_Task_Priority", "[Priority] BETWEEN 0 AND 3");
            });
            e.Property(x => x.Title).HasMaxLength(160).IsRequired();
            e.Property(x => x.Description).HasMaxLength(5000);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ProjectMember>().WithMany().HasForeignKey(x => new { x.ProjectId, x.AssigneeId }).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ProjectId, x.Status });
            e.HasIndex(x => new { x.AssigneeId, x.DueDate });
        });
        b.Entity<TaskComment>(e =>
        {
            e.Property(x => x.Body).HasMaxLength(3000).IsRequired();
            e.HasOne<TaskItem>().WithMany().HasForeignKey(x => x.TaskItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TaskItemId, x.CreatedAt });
        });
        b.Entity<ActivityLog>(e =>
        {
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<TaskItem>().WithMany().HasForeignKey(x => x.TaskItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ProjectId, x.CreatedAt });
        });
        b.Entity<Notification>(e =>
        {
            e.Property(x => x.Message).HasMaxLength(300).IsRequired();
            e.Property(x => x.EventKey).HasMaxLength(180).IsRequired();
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<TaskItem>().WithMany().HasForeignKey(x => x.TaskItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.UserId, x.EventKey }).IsUnique();
            e.HasIndex(x => new { x.UserId, x.ReadAt });
        });
        foreach (var entity in b.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(string) && property.Name.EndsWith("Id")) property.SetMaxLength(128);
            }
            if (typeof(Entity).IsAssignableFrom(entity.ClrType))
                b.Entity(entity.ClrType).Property(nameof(Entity.Version)).IsConcurrencyToken();
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Entity>().Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.UpdatedAt = clock.GetUtcNow();
            entry.Entity.Version = Guid.NewGuid();
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = entry.Entity.UpdatedAt;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
