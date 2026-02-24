using System.Security.Claims;
using FemCircleProject.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FemCircleProject.Services;

public sealed class AccountService : IAccountService
{
    private readonly InMemoryAppStore _store;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountService(InMemoryAppStore store, IHttpContextAccessor httpContextAccessor)
    {
        _store = store;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<RegisterResult> RegisterAsync(RegisterViewModel model, CancellationToken cancellationToken = default)
    {
        if (!_store.TryCreateUser(model, out UserRecord? createdUser, out string? errorMessage))
        {
            return new RegisterResult(false, errorMessage);
        }

        if (createdUser is not null)
        {
            await SignInInternalAsync(createdUser, false);
        }

        return new RegisterResult(true);
    }

    public async Task<bool> PasswordSignInAsync(LoginViewModel model, CancellationToken cancellationToken = default)
    {
        UserRecord? user = _store.GetUserForSignIn(model.UsernameOrEmail.Trim(), model.Password);
        if (user is null || user.IsBlocked)
        {
            return false;
        }

        await SignInInternalAsync(user, model.RememberMe);
        return true;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        if (_httpContextAccessor.HttpContext is null)
        {
            return;
        }

        await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private async Task SignInInternalAsync(UserRecord user, bool rememberMe)
    {
        if (_httpContextAccessor.HttpContext is null)
        {
            return;
        }

        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("full_name", user.FullName)
        };

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new(identity);

        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await _httpContextAccessor.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);
    }
}
