using FemCircleProject.Services;
using FemCircleProject.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemCircleProject.Controllers;

public sealed class AccountController : Controller
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel
        {
            AcceptTerms = true
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        bool accepted = model.AcceptTerms;

        if (Request.HasFormContentType && Request.Form.TryGetValue(nameof(RegisterViewModel.AcceptTerms), out var acceptValues))
        {
            accepted = acceptValues.Any(v =>
                string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v, "on", StringComparison.OrdinalIgnoreCase));
        }

        model.AcceptTerms = accepted;
        if (!accepted)
        {
            ModelState.AddModelError(nameof(RegisterViewModel.AcceptTerms), "You must accept the terms and conditions.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        RegisterResult result;

        try
        {
            result = await _accountService.RegisterAsync(model, cancellationToken);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Registration failed due to an unexpected error. Please try again.");
            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Registration failed.");
            return View(model);
        }

        return RedirectToAction("Index", "Products");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        bool signedIn = await _accountService.PasswordSignInAsync(model, cancellationToken);
        if (!signedIn)
        {
            ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Products");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _accountService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(Login));
    }
}
