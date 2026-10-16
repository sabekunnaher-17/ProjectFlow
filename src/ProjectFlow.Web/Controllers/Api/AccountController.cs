using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectFlow.Core.DTOs;
using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Controllers.Api;

[ApiController, Route("api/v1/account"), Authorize]
public sealed class AccountController(AccountService accounts, IAntiforgery csrf) : ControllerBase
{
    [HttpGet("csrf"), AllowAnonymous, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Token() => Ok(new { token = csrf.GetAndStoreTokens(HttpContext).RequestToken });
    [HttpPost("register"), AllowAnonymous, EnableRateLimiting("account")]
    public async Task<IActionResult> Register(RegisterRequest input, CancellationToken ct) { await accounts.RegisterAsync(input, ct); return StatusCode(201, new { message = "Account created. Sign in to continue." }); }
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("account")]
    public async Task<IActionResult> Login(LoginRequest input) => await accounts.LoginAsync(input) ? Ok(new { message = "Signed in." }) : Problem(statusCode: 401, title: "Sign in failed. Check your credentials or try again later.");
    [HttpPost("logout")] public async Task<IActionResult> Logout() { await accounts.LogoutAsync(); return NoContent(); }
    [HttpGet("me")] public Task<AccountDto> Me() => accounts.GetAsync(User);
    [HttpPut("profile")] public async Task<IActionResult> Profile(ProfileRequest input) { await accounts.UpdateAsync(User, input); return NoContent(); }
}
