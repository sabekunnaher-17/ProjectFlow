using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectFlow.Core.Enums;
using ProjectFlow.Core.Interfaces;
using ProjectFlow.Infrastructure.Data;
using ProjectFlow.Infrastructure.Services;
using ProjectFlow.Web.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi("v1", o => o.ShouldInclude = d => d.RelativePath?.StartsWith("api/v1/", StringComparison.Ordinal) == true);
builder.Services.AddExceptionHandler<ExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<ApplicationDbContext>(o =>
{
    var connection = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(connection))
        o.UseSqlServer(connection);
});
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o =>
{
    o.Password.RequiredLength = 12;
    o.User.RequireUniqueEmail = true;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<ApplicationDbContext>().AddSignInManager<ActiveSignInManager>().AddDefaultTokenProviders();
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "ProjectFlow.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/Denied";
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = false;
    o.Events.OnRedirectToLogin = c => { if (c.Request.Path.StartsWithSegments("/api")) c.Response.StatusCode = 401; else c.Response.Redirect(c.RedirectUri); return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = c => { if (c.Request.Path.StartsWithSegments("/api")) c.Response.StatusCode = 403; else c.Response.Redirect(c.RedirectUri); return Task.CompletedTask; };
});
builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("account", context => RateLimitPartition.GetFixedWindowLimiter(
        // Forwarded headers are deliberately not trusted by this single-host app.
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddScoped<AccountService>();
builder.Services.AddSingleton<UnknownPasswordVerifier>();
builder.Services.AddScoped<IActor, HttpActor>();
builder.Services.AddScoped<AccessService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<InsightService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<NotificationEvents>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<DeadlineService>();
builder.Services.AddScoped<AdminService>();
if (builder.Configuration.GetValue("Deadlines:Enabled", true)) builder.Services.AddHostedService<ProjectFlow.Web.Services.DeadlineWorker>();

var app = builder.Build();
if (args.Contains("--bootstrap-admin"))
{
    using var scope = app.Services.CreateScope();
    var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var email = app.Configuration["Bootstrap:Email"] ?? throw new InvalidOperationException("Set Bootstrap__Email for an existing account.");
    await accounts.EnsureRolesAsync();
    var user = await users.FindByEmailAsync(email);
    if (user is null || !user.IsActive || (await users.GetUsersInRoleAsync(Roles.Admin)).Count != 0)
        throw new InvalidOperationException("Bootstrap requires an active existing account and no existing administrator.");
    if (!(await users.AddToRoleAsync(user, Roles.Admin)).Succeeded)
        throw new InvalidOperationException("Admin bootstrap failed.");
    await users.UpdateSecurityStampAsync(user);
    Console.WriteLine("Administrator bootstrap completed. Sign in again.");
    return;
}
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseWhen(c => !c.Request.Path.StartsWithSegments("/api"), branch => branch.UseStatusCodePagesWithReExecute("/Home/Status/{0}"));
if (app.Environment.IsDevelopment()) app.MapOpenApi().AllowAnonymous();

app.MapStaticAssets().AllowAnonymous();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

public partial class Program
{
}
