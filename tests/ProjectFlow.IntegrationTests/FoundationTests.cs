using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace ProjectFlow.IntegrationTests;

public sealed class FoundationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;
    public FoundationTests(WebApplicationFactory<Program> factory) =>
        client = factory.WithWebHostBuilder(b => b.UseEnvironment("Development")).CreateClient();

    [Fact]
    public async Task LivenessDoesNotRequireDatabaseAccess()
    {
        var response = await client.GetAsync("/health/live");
        response.EnsureSuccessStatusCode();
        Assert.Contains("healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LandingPageRendersAndSuppliesSecurityHeaders()
    {
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        Assert.Contains("ProjectFlow", await response.Content.ReadAsStringAsync());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task DevelopmentOpenApiDocumentsOnlyTheVersionedJsonApi()
    {
        var response = await client.GetAsync("/openapi/v1.json"); response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/v1/projects", text); Assert.Contains("/api/v1/notifications", text);
        Assert.DoesNotContain("/Account/Login", text);
        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/css/site.css")).StatusCode);
    }
}
