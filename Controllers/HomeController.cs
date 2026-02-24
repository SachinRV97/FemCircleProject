using System.Diagnostics;
using FemCircleProject.Models;
using FemCircleProject.Services;
using FemCircleProject.ViewModels.Product;
using Microsoft.AspNetCore.Mvc;

namespace FemCircleProject.Controllers;

public sealed class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProductService _productService;

    public HomeController(ILogger<HomeController> logger, IProductService productService)
    {
        _logger = logger;
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ProductSearchViewModel search, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            search = new ProductSearchViewModel();
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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
