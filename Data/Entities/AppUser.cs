namespace FemCircleProject.Data.Entities;

public sealed class AppUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public bool IsVerified { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime RegisteredOnUtc { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
