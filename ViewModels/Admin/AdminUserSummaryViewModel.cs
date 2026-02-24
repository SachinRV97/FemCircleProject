namespace FemCircleProject.ViewModels.Admin;

public sealed class AdminUserSummaryViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime RegisteredOnUtc { get; set; }
}
