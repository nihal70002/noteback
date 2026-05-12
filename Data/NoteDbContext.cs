using Microsoft.EntityFrameworkCore;
using Note.Backend.Models;
using System.Collections.Generic;

namespace Note.Backend.Data;

public class NoteDbContext : DbContext
{
    public NoteDbContext(DbContextOptions<NoteDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<ProductReview> ProductReviews { get; set; }
    public DbSet<PackChoice> PackChoices { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<BusinessExpense> BusinessExpenses { get; set; }
    public DbSet<StorefrontConfig> StorefrontConfigs { get; set; }
    public DbSet<ShippingSettings> ShippingSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed Admin user (Password: Password123!)
        modelBuilder.Entity<User>().HasData(
            new User 
            { 
                Id = "admin-user-id", 
                PhoneNumber = "admin@note.com", 
                PasswordHash = "$2a$11$H4Gu44Jwzu4Nx8EClozYu.dczwh4JWiDDtkMmdutbdprmG/f9hMBe",
                Role = "Admin"
            }
        );

        modelBuilder.Entity<Coupon>().HasKey(c => c.Code);

        modelBuilder.Entity<WishlistItem>()
            .HasIndex(w => new { w.UserId, w.ProductId })
            .IsUnique();

        modelBuilder.Entity<ProductReview>()
            .HasIndex(r => new { r.UserId, r.ProductId })
            .IsUnique();

        modelBuilder.Entity<PackChoice>()
            .HasOne(pc => pc.PackProduct)
            .WithMany()
            .HasForeignKey(pc => pc.PackProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PackChoice>()
            .HasOne(pc => pc.ChoiceProduct)
            .WithMany()
            .HasForeignKey(pc => pc.ChoiceProductId)
            .OnDelete(DeleteBehavior.NoAction);

        // Seed initial products
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = "1", Name = "The Minimalist Grid", Price = 28.00m, Image = "/product.png", Category = "Journals", IsNew = true, Stock = 25, Description = "A meticulously crafted daily journal featuring high-grade, acid-free 120gsm paper. The subtle 5mm dot grid provides structure without constraint, perfect for bullet journaling, sketching, or structured noting. Encased in a premium linen finish hard cover." },
            new Product { Id = "2", Name = "Midnight Leather", Price = 45.00m, Image = "/product1.png", Category = "Premium", IsNew = false, Stock = 12 },
            new Product { Id = "3", Name = "Taupe Linen Planner", Price = 32.00m, Image = "/product2.png", Category = "Planners", IsNew = true, Stock = 18 },
            new Product { Id = "4", Name = "Pocket Ideas Book", Price = 18.00m, Image = "/product3.png", Category = "Pocket", IsNew = false, Stock = 30 },
            new Product { Id = "5", Name = "Weekly Overview", Price = 24.00m, Image = "/product4.png", Category = "Planners", IsNew = false, Stock = 16 },
            new Product { Id = "6", Name = "Dotted Sketch Pad", Price = 22.00m, Image = "/product5.png", Category = "Creative", IsNew = false, Stock = 20 },
            new Product { Id = "7", Name = "Morning Pages", Price = 26.00m, Image = "/product6.png", Category = "Journals", IsNew = true, Stock = 24 },
            new Product { Id = "8", Name = "The Master Collection", Price = 85.00m, Image = "/product7.png", Category = "Premium", IsNew = false, Stock = 8 }
        );

        modelBuilder.Entity<Coupon>().HasData(
            new Coupon { Code = "WELCOME10", DiscountPercent = 10m, IsActive = true },
            new Coupon { Code = "PAPER15", DiscountPercent = 15m, IsActive = true }
        );
    }
}
