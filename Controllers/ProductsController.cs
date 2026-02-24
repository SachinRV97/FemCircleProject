using FemCircleProject.Services;
using FemCircleProject.ViewModels.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemCircleProject.Controllers;

[Authorize]
public sealed class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProductsController(IProductService productService, IWebHostEnvironment webHostEnvironment)
    {
        _productService = productService;
        _webHostEnvironment = webHostEnvironment;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ProductSearchViewModel search, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(new ProductIndexViewModel { Search = search });
        }

        ProductIndexViewModel model = await _productService.BuildIndexAsync(search, cancellationToken);

        string? currentUserName = User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(currentUserName))
        {
            List<ProductListItemViewModel> filtered = model.Products
                .Where(p => !string.Equals(p.SellerUserName, currentUserName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            model.Products = filtered;
            model.TotalCount = filtered.Count;
        }

        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        ProductDetailsViewModel? model = await _productService.GetDetailsAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        string? currentUserName = User.Identity?.Name;
        bool isAdmin = User.IsInRole("Admin");
        bool isOwner = !string.IsNullOrWhiteSpace(currentUserName) &&
                       string.Equals(model.SellerUserName, currentUserName, StringComparison.OrdinalIgnoreCase);
        bool isBuyer = !string.IsNullOrWhiteSpace(currentUserName) &&
                       !string.IsNullOrWhiteSpace(model.BoughtByUserName) &&
                       string.Equals(model.BoughtByUserName, currentUserName, StringComparison.OrdinalIgnoreCase);
        bool isBookingPending = !model.IsSold && !string.IsNullOrWhiteSpace(model.BoughtByUserName);

        model.CanManage = isAdmin || isOwner;
        model.IsBoughtByCurrentUser = isBuyer;
        model.IsBookingPending = isBookingPending;
        model.IsBookingPendingForCurrentUser = isBuyer && isBookingPending;
        model.CanBook = (User.Identity?.IsAuthenticated ?? false) && !isAdmin && !isOwner && !model.IsSold && !isBookingPending;
        model.CanReviewBooking = isOwner && isBookingPending;
        model.CanUndoBooking = model.IsSold && model.CanManage;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyActivity(CancellationToken cancellationToken)
    {
        ProductActivityDashboardViewModel? model = await _productService.GetMyActivityAsync(User.Identity?.Name, cancellationToken);
        if (model is null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(int id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin"))
        {
            return Forbid();
        }

        bool success = await _productService.BookAsync(id, User.Identity?.Name, cancellationToken);
        TempData[success ? "ProductSuccess" : "ProductError"] = success
            ? "Booking request sent to owner. It will be visible after owner approval."
            : "Unable to book this product. It may already have a pending booking or sold.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveBooking(int id, CancellationToken cancellationToken)
    {
        bool success = await _productService.ApproveBookingAsync(id, User.Identity?.Name, cancellationToken);
        TempData[success ? "ProductSuccess" : "ProductError"] = success
            ? "Booking approved. Product is now marked as sold."
            : "Unable to approve booking for this product.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBooking(int id, CancellationToken cancellationToken)
    {
        bool success = await _productService.RejectBookingAsync(id, User.Identity?.Name, cancellationToken);
        TempData[success ? "ProductSuccess" : "ProductError"] = success
            ? "Booking rejected. Product is available again."
            : "Unable to reject booking for this product.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UndoBooking(int id, CancellationToken cancellationToken)
    {
        bool success = await _productService.UndoBookingAsync(id, User.Identity?.Name, cancellationToken);
        TempData[success ? "ProductSuccess" : "ProductError"] = success
            ? "Booking has been undone. Listing is active again."
            : "Unable to undo booking for this product.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProductCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        (bool success, string? imagePath, string? errorMessage) = await SaveProductImageAsync(model.ImageFile, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(nameof(model.ImageFile), errorMessage ?? "Image upload failed.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            model.ImageUrl = imagePath;
        }

        string currentUser = User.Identity?.Name ?? string.Empty;
        int newId = await _productService.CreateAsync(model, currentUser, cancellationToken);

        return RedirectToAction(nameof(Details), new { id = newId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ProductEditViewModel? model = await _productService.GetForEditAsync(id, User.Identity?.Name, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        (bool success, string? imagePath, string? errorMessage) = await SaveProductImageAsync(model.ImageFile, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(nameof(model.ImageFile), errorMessage ?? "Image upload failed.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            model.ImageUrl = imagePath;
        }

        bool updated = await _productService.UpdateAsync(model, User.Identity?.Name, cancellationToken);
        if (!updated)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        bool deleted = await _productService.DeleteAsync(id, User.Identity?.Name, cancellationToken);
        if (!deleted)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<(bool Success, string? Path, string? Error)> SaveProductImageAsync(IFormFile? imageFile, CancellationToken cancellationToken)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return (true, null, null);
        }

        const long maxFileSize = 5 * 1024 * 1024;
        if (imageFile.Length > maxFileSize)
        {
            return (false, null, "Image size must be 5 MB or less.");
        }

        string extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        HashSet<string> allowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

        if (!allowedExtensions.Contains(extension))
        {
            return (false, null, "Only JPG, JPEG, PNG, and WEBP files are allowed.");
        }

        if (string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath))
        {
            return (false, null, "Server storage path is not available.");
        }

        string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadFolder);

        string fileName = $"{Guid.NewGuid():N}{extension}";
        string filePath = Path.Combine(uploadFolder, fileName);

        await using FileStream stream = new(filePath, FileMode.Create);
        await imageFile.CopyToAsync(stream, cancellationToken);

        return (true, $"/uploads/products/{fileName}", null);
    }
}
