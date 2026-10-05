using System.Security.Claims;
using JudgeMyTaste.Models;
using JudgeMyTaste.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JudgeMyTaste.Controllers;

public class AccountController : Controller
{
    private static readonly AppUser DummyUser = new(0, "dummy", "", null, "User");
    private static readonly string DummyHash =
        new PasswordHasher<AppUser>().HashPassword(DummyUser, "Dummy#Password1");

    private readonly UserRepository _users;
    private readonly IPasswordHasher<AppUser> _hasher;

    public AccountController(UserRepository users, IPasswordHasher<AppUser> hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    // ---------- LOGIN ----------
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Musik");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.GetByUsernameAsync(model.Username.Trim());

        var result = _hasher.VerifyHashedPassword(
            user ?? DummyUser,
            user?.PasswordHash ?? DummyHash,
            model.Password);

        if (user is null || result == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Username atau password salah.");
            return View(model);
        }

        await SignInUserAsync(user, model.RememberMe);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Musik");
    }

    // ---------- REGISTER ----------
    [HttpGet, AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Musik");

        return View(new RegisterViewModel());
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var username = model.Username.Trim();
        var fullName = string.IsNullOrWhiteSpace(model.FullName) ? null : model.FullName.Trim();

        var hash = _hasher.HashPassword(
            new AppUser(0, username, "", fullName, "User"),
            model.Password);

        var id = await _users.CreateAsync(username, hash, fullName);
        if (id == 0)
        {
            ModelState.AddModelError(nameof(model.Username), "Username sudah dipakai.");
            return View(model);
        }

        TempData["Info"] = "Akun berhasil dibuat. Silakan login.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- LOGOUT ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // ---------- REJECTED ----------
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task SignInUserAsync(AppUser user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new("FullName", user.FullName ?? user.Username)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(rememberMe ? 7 : 1)
            });
    }
}
