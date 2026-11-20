using Microsoft.EntityFrameworkCore;
using Backend_SaveMyMeal.Models.Entities;

namespace Backend_SaveMyMeal.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Restaurant> Restaurants { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Meal> Meals { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; } = null!;
        public DbSet<QrCode> QrCodes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();
            });

            // Restaurant relationships
            modelBuilder.Entity<Restaurant>(entity =>
            {
                entity.HasOne(r => r.Owner)
                      .WithMany(u => u.Restaurants)
                      .HasForeignKey(r => r.OwnerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Meal relationships
            modelBuilder.Entity<Meal>(entity =>
            {
                entity.HasOne(m => m.Restaurant)
                      .WithMany(r => r.Meals)
                      .HasForeignKey(m => m.RestaurantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Category)
                      .WithMany(c => c.Meals)
                      .HasForeignKey(m => m.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Order relationships
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasOne(o => o.Client)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(o => o.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Restaurant)
                      .WithMany(r => r.Orders)
                      .HasForeignKey(o => o.RestaurantId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // OrderItem relationships
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasOne(oi => oi.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Meal)
                      .WithMany(m => m.OrderItems)
                      .HasForeignKey(oi => oi.MealId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // QrCode relationships
            modelBuilder.Entity<QrCode>(entity =>
            {
                entity.HasIndex(q => q.CodeValue).IsUnique();

                entity.HasOne(q => q.Order)
                      .WithOne(o => o.QrCode)
                      .HasForeignKey<QrCode>(q => q.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // LoyaltyAccount relationships
            modelBuilder.Entity<LoyaltyAccount>(entity =>
            {
                entity.HasOne(l => l.User)
                      .WithOne(u => u.LoyaltyAccount)
                      .HasForeignKey<LoyaltyAccount>(l => l.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
