using System.Security.Claims;
using FemCircleProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemCircleProject.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminController : Controller
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var model = await _adminService.GetDashboardAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        var users = await _adminService.GetUsersAsync(cancellationToken);
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> PendingListings(CancellationToken cancellationToken)
    {
        var products = await _adminService.GetPendingProductsAsync(cancellationToken);
        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveListing(int id, string? returnAction, CancellationToken cancellationToken)
    {
        bool success = await _adminService.ApproveListingAsync(id, cancellationToken);
        TempData[success ? "AdminSuccess" : "AdminError"] = success
            ? "Listing approved successfully."
            : "Unable to approve listing.";

        return RedirectToAction(NormalizeReturnAction(returnAction));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectListing(int id, string? returnAction, CancellationToken cancellationToken)
    {
        bool success = await _adminService.RejectListingAsync(id, cancellationToken);
        TempData[success ? "AdminSuccess" : "AdminError"] = success
            ? "Listing rejected successfully."
            : "Unable to reject listing.";

        return RedirectToAction(NormalizeReturnAction(returnAction));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserVerification(int id, CancellationToken cancellationToken)
    {
        bool success = await _adminService.ToggleUserVerificationAsync(id, cancellationToken);
        TempData[success ? "AdminSuccess" : "AdminError"] = success
            ? "User verification status updated."
            : "Unable to update user verification status.";

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserBlocked(int id, CancellationToken cancellationToken)
    {
        int adminId = GetCurrentUserId();
        bool success = await _adminService.ToggleUserBlockedAsync(id, adminId, cancellationToken);
        TempData[success ? "AdminSuccess" : "AdminError"] = success
            ? "User block status updated."
            : "Unable to update user block status.";

        return RedirectToAction(nameof(Users));
    }

    private static string NormalizeReturnAction(string? returnAction)
    {
        return string.Equals(returnAction, nameof(Dashboard), StringComparison.OrdinalIgnoreCase)
            ? nameof(Dashboard)
            : nameof(PendingListings);
    }

    private int GetCurrentUserId()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out int value) ? value : 0;
    }
}
