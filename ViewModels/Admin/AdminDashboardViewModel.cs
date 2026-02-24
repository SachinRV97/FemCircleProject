namespace FemCircleProject.ViewModels.Admin;

public sealed class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveListings { get; set; }
    public int PendingModerationCount { get; set; }
    public int CompletedOrders { get; set; }
    public IReadOnlyList<AdminProductSummaryViewModel> RecentListings { get; set; } = Array.Empty<AdminProductSummaryViewModel>();
    public IReadOnlyList<AdminUserSummaryViewModel> NewlyRegisteredUsers { get; set; } = Array.Empty<AdminUserSummaryViewModel>();
}
