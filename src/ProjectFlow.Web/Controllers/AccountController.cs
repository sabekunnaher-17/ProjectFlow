using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectFlow.Core.Common;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers;

[Authorize]
public sealed class AccountController(AccountService accounts) : Controller
{
    [AllowAnonymous, HttpGet] public IActionResult Register() => View(new RegisterRequest());
    [AllowAnonymous, HttpPost, EnableRateLimiting("account")]
    public async Task<IActionResult> Register(RegisterRequest input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(input);
        try { await accounts.RegisterAsync(input, ct); return RedirectToAction(nameof(Login)); }
        catch (DomainException e) { ModelState.AddModelError("", e.Message); return View(input); }
    }

    [AllowAnonymous, HttpGet] public IActionResult Login() => View(new LoginRequest());
    [AllowAnonymous, HttpPost, EnableRateLimiting("account")]
    public async Task<IActionResult> Login(LoginRequest input)
    {
        if (!ModelState.IsValid) return View(input);
        if (await accounts.LoginAsync(input)) return RedirectToAction("Index", "Home");
        ModelState.AddModelError("", "Sign in failed. Check your credentials or try again later.");
        return View(input);
    }

    [HttpPost] public async Task<IActionResult> Logout() { await accounts.LogoutAsync(); return RedirectToAction("Index", "Home"); }
    [HttpGet] public async Task<IActionResult> Profile() => View(new ProfileRequest { DisplayName = (await accounts.GetAsync(User)).DisplayName });
    [HttpPost]
    public async Task<IActionResult> Profile(ProfileRequest input)
    {
        if (!ModelState.IsValid) return View(input);
        await accounts.UpdateAsync(User, input);
        TempData["Success"] = "Profile saved.";
        return RedirectToAction(nameof(Profile));
    }
    [AllowAnonymous, HttpGet] public IActionResult Denied() { Response.StatusCode = 403; return View(); }
}
