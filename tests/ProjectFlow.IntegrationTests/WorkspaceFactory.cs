using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectFlow.Infrastructure.Data;

namespace ProjectFlow.IntegrationTests;

// SQLite is ONLY an isolated, in-memory HTTP test provider. Production uses SQL Server.
public sealed class WorkspaceFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly string? server = Environment.GetEnvironmentVariable("PROJECTFLOW_TEST_SQL_SERVER");
    private readonly string databaseName = $"ProjectFlow_{Guid.NewGuid():N}_test";
    private string SqlTarget => new SqlConnectionStringBuilder(server) { InitialCatalog = databaseName }.ConnectionString;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            if (string.IsNullOrEmpty(server))
            {
                connection.Open();
                services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(connection));
            }
            else services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(SqlTarget));
        });
    }

    public async Task InitializeAsync()
    {
        if (!string.IsNullOrEmpty(server))
        {
            // Never reuse, delete, migrate or seed an existing database.
            if (!databaseName.EndsWith("_test") || databaseName.Length != 49) throw new InvalidOperationException("Invalid isolated database name.");
            await using var master = new SqlConnection(new SqlConnectionStringBuilder(server) { InitialCatalog = "master" }.ConnectionString);
            await master.OpenAsync();
            await using var check = master.CreateCommand();
            check.CommandText = "SELECT DB_ID(@name)"; check.Parameters.AddWithValue("@name", databaseName);
            if (await check.ExecuteScalarAsync() is not DBNull) throw new InvalidOperationException("Test database name already exists; refusing reuse.");
            await using var create = master.CreateCommand();
            create.CommandText = $"CREATE DATABASE [{databaseName}]"; await create.ExecuteNonQueryAsync();
        }
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (string.IsNullOrEmpty(server)) await db.Database.EnsureCreatedAsync();
        else
        {
            Assert.Equal(databaseName, db.Database.GetDbConnection().Database);
            await db.Database.MigrateAsync();
        }
    }
    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    public static async Task TokenAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/v1/account/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
    }
    public static async Task RegisterAsync(HttpClient client, string email, string name = "Test Member")
    {
        await TokenAsync(client);
        var response = await client.PostAsJsonAsync("/api/v1/account/register", new { displayName = name, email, password = "Test-only!Secret42" });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }
    public static async Task LoginAsync(HttpClient client, string email)
    {
        await TokenAsync(client);
        var response = await client.PostAsJsonAsync("/api/v1/account/login", new { email, password = "Test-only!Secret42" });
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        await TokenAsync(client); // Token is bound to the authenticated identity.
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}
