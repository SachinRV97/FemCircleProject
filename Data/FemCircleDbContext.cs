using FemCircleProject.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FemCircleProject.Data;

public sealed class FemCircleDbContext : DbContext
{
    public FemCircleDbContext(DbContextOptions<FemCircleDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.UserName).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(20);
            entity.Property(x => x.City).HasMaxLength(80);

            entity.HasIndex(x => x.UserName).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(3000).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(60).IsRequired();
            entity.Property(x => x.ItemCondition).HasMaxLength(40).IsRequired();
            entity.Property(x => x.City).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ImageUrl).HasMaxLength(600);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.IsSold).HasDefaultValue(false);

            entity.HasOne(x => x.Seller)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.BoughtByUser)
                .WithMany(x => x.BoughtProducts)
                .HasForeignKey(x => x.BoughtByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
