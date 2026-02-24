using FemCircleProject.ViewModels.Account;

namespace FemCircleProject.Services;

public interface IAccountService
{
    Task<RegisterResult> RegisterAsync(RegisterViewModel model, CancellationToken cancellationToken = default);
    Task<bool> PasswordSignInAsync(LoginViewModel model, CancellationToken cancellationToken = default);
    Task SignOutAsync(CancellationToken cancellationToken = default);
}

public sealed record RegisterResult(bool Succeeded, string? ErrorMessage = null);
