using Microsoft.Playwright;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.Playwright.Assertions;

namespace ProjectFlow.IntegrationTests;

public sealed class BrowserTests
{
    [BrowserFact]
    public async Task RealMvcFormsFetchAndResponsiveNavigationCompleteAWorkspaceJourney()
    {
        using var app = new WorkspaceFactory(); app.UseKestrel(0); await app.InitializeAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        Assert.NotEqual(80, new Uri(address).Port);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 }, BaseURL = address });
        var page = await context.NewPageAsync(); var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        await page.GotoAsync("/Account/Register");
        await page.GetByLabel("Full name").FillAsync("Browser Member"); await page.GetByLabel("Email", new() { Exact = true }).FillAsync("browser@example.test"); await page.GetByLabel("Password", new() { Exact = true }).FillAsync("Test-only!Secret42");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create account", Exact = true }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/Login$"));
        await page.GetByLabel("Email", new() { Exact = true }).FillAsync("browser@example.test"); await page.GetByLabel("Password", new() { Exact = true }).FillAsync("Test-only!Secret42");
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Overview", Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace", Exact = true }).GetByRole(AriaRole.Link, new() { Name = "Organizations", Exact = true }).ClickAsync();
        await page.GetByLabel("Organization name").FillAsync("Browser workspace"); await page.GetByRole(AriaRole.Button, new() { Name = "Create workspace", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "New project", Exact = true }).ClickAsync();
        await page.GetByLabel("Name", new() { Exact = true }).FillAsync("Browser project");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save project", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Browser project", Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Project tasks", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "New task", Exact = true }).ClickAsync();
        await page.GetByLabel("Title", new() { Exact = true }).FillAsync("Browser task");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save task", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Browser task", Exact = true })).ToBeVisibleAsync();
        await page.GetByLabel("Move task").SelectOptionAsync("1");
        await page.GetByRole(AriaRole.Button, new() { Name = "Update status", Exact = true }).ClickAsync();
        await Expect(page.Locator(".eyebrow")).ToContainTextAsync("InProgress");
        await page.GetByLabel("Add a comment", new() { Exact = true }).FillAsync("A real browser comment."); await page.GetByRole(AriaRole.Button, new() { Name = "Post comment", Exact = true }).ClickAsync();
        await Expect(page.GetByText("A real browser comment.", new() { Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Edit task", Exact = true }).ClickAsync();
        await Expect(page.GetByLabel("Title", new() { Exact = true })).ToHaveValueAsync("Browser task");
        await page.GetByLabel("Title", new() { Exact = true }).FillAsync("Browser task updated"); await page.GetByRole(AriaRole.Button, new() { Name = "Save task", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Browser task updated", Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Browser project tasks", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Board view", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Kanban board", Exact = true })).ToBeVisibleAsync();
        await page.SetViewportSizeAsync(390, 844);
        await page.GetByRole(AriaRole.Button, new() { Name = "Toggle workspace navigation", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace", Exact = true }).GetByRole(AriaRole.Link, new() { Name = "Overview", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Overview", Exact = true })).ToBeVisibleAsync();
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth"));
        var screenshots = Environment.GetEnvironmentVariable("PROJECTFLOW_SCREENSHOTS");
        if (!string.IsNullOrEmpty(screenshots))
        {
            Directory.CreateDirectory(screenshots);
            await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "projectflow-mobile.png"), FullPage = true });
            await page.SetViewportSizeAsync(1440, 1000);
            await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "projectflow-desktop.png"), FullPage = true });
        }
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign out", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Sign in", Exact = true }).First).ToBeVisibleAsync();
        Assert.Empty(errors);
        await context.CloseAsync();
    }
}

public sealed class BrowserFactAttribute : FactAttribute
{
    public BrowserFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("PROJECTFLOW_BROWSER_TESTS") != "1") Skip = "Real Chromium journey runs explicitly in CI; no local browser installation authorized.";
    }
}
