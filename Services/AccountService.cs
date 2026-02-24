using System.Security.Claims;
using FemCircleProject.Data;
using FemCircleProject.Data.Entities;
using FemCircleProject.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FemCircleProject.Services;

public sealed class AccountService : IAccountService
{
    private readonly FemCircleDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public AccountService(
        FemCircleDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        IPasswordHasher<AppUser> passwordHasher)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterResult> RegisterAsync(RegisterViewModel model, CancellationToken cancellationToken = default)
    {
        string userName = model.UserName.Trim();
        string normalizedUserName = userName.ToLowerInvariant();
        string email = model.Email.Trim().ToLowerInvariant();

        if (await _dbContext.Users.AnyAsync(u => u.UserName.ToLower() == normalizedUserName, cancellationToken))
        {
            return new RegisterResult(false, "Username already exists.");
        }

        if (await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == email, cancellationToken))
        {
            return new RegisterResult(false, "Email already exists.");
        }

        AppUser user = new()
        {
            FullName = model.FullName.Trim(),
            UserName = userName,
            Email = email,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim(),
            IsVerified = true,
            IsBlocked = false,
            IsAdmin = false,
            RegisteredOnUtc = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new RegisterResult(false, "Username or email already exists. Please use different details.");
        }

        await SignInInternalAsync(user, false);
        return new RegisterResult(true);
    }

    public async Task<bool> PasswordSignInAsync(LoginViewModel model, CancellationToken cancellationToken = default)
    {
        string loginValue = model.UsernameOrEmail.Trim();
        string normalizedUserName = loginValue.ToLowerInvariant();
        string normalizedEmail = loginValue.ToLowerInvariant();

        AppUser? user = await _dbContext.Users.FirstOrDefaultAsync(
            u => u.UserName.ToLower() == normalizedUserName || u.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (user is null || user.IsBlocked)
        {
            return false;
        }

        PasswordVerificationResult verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return false;
        }

        if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            await _dbContext.SaveChangesAsync(cancellationToken);
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

    private async Task SignInInternalAsync(AppUser user, bool rememberMe)
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
