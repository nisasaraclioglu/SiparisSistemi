using Microsoft.EntityFrameworkCore;
using SiparisSistemi.Helpers;
using SiparisSistemi.Models;

namespace SiparisSistemi.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet tanımlamaları
        public DbSet<Customers> Customers { get; set; }
        public DbSet<Products> Products { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<Logs> Logs { get; set; }
        public DbSet<Admin> Admin { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Customers tablosu için konfigürasyon
            modelBuilder.Entity<Customers>(entity =>
            {
                entity.HasKey(e => e.CustomerID);
                entity.Property(e => e.CustomerName).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Budget).IsRequired();
                entity.Property(e => e.CustomerType).IsRequired();
                entity.Property(e => e.TotalSpent).HasDefaultValue(0);
            });

            // Products tablosu için konfigürasyon
            modelBuilder.Entity<Products>(entity =>
            {
                entity.HasKey(e => e.ProductID);
                entity.Property(e => e.ProductName)
                      .IsRequired()
                      .HasMaxLength(255);
                entity.Property(e => e.Stock).IsRequired();
                entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(10,2)");
                entity.Property(e => e.ProductType)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(e => e.ImagePath).HasMaxLength(255);
            });

            // Orders tablosu için konfigürasyon
            modelBuilder.Entity<Orders>(entity =>
            {
                entity.HasKey(e => e.OrderID);
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.TotalPrice).IsRequired().HasColumnType("decimal(10,2)");
                entity.Property(e => e.OrderDate).IsRequired();
                entity.Property(e => e.OrderStatus).IsRequired();

                entity.HasOne(d => d.Customer)
                      .WithMany(p => p.Orders)
                      .HasForeignKey(d => d.CustomerID);

                entity.HasOne(d => d.Product)
                      .WithMany(p => p.Orders)
                      .HasForeignKey(d => d.ProductID);
            });

            // Logs tablosu için konfigürasyon
            modelBuilder.Entity<Logs>(entity =>
            {
                entity.HasKey(e => e.LogID);
                entity.Property(e => e.LogDate).IsRequired();
                entity.Property(e => e.LogType).IsRequired();
                entity.Property(e => e.LogDetails).IsRequired();

                entity.HasOne(d => d.Customer)
                      .WithMany(p => p.Logs)
                      .HasForeignKey(d => d.CustomerID);

                entity.HasOne(d => d.Order)
                      .WithMany()
                      .HasForeignKey(d => d.OrderID);
            });

            // Admin tablosu için konfigürasyon
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.HasKey(e => e.AdminID);
                entity.Property(e => e.Username).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
            });

            // Varsayılan bir Admin eklemek için
            modelBuilder.Entity<Admin>().HasData(
                new Admin
                {
                    AdminID = 1,
                    Username = "admin",
                    PasswordHash = HashHelper.HashPassword("1") // Şifre hashleniyor
                }
            );
        }
        public static void SeedRandomCustomers(ApplicationDbContext context)
        {
            // Mevcut müşterileri temizle (isteğe bağlı)
            context.Customers.RemoveRange(context.Customers);
            context.SaveChanges();

            // Rastgele müşteri oluştur
            Random random = new Random();
            int customerCount = random.Next(5, 11);

            var customers = new List<Customers>();

            for (int i = 0; i < customerCount; i++)
            {
                customers.Add(new Customers
                {
                    CustomerName = $"Customer{i + 1}",
                    Budget = random.Next(500, 3001),
                    CustomerType = i < 2 ? "Premium" : "Standard", // İlk iki müşteri premium
                    TotalSpent = 0,
                    PasswordHash = HashHelper.HashPassword("1") // Şifre "1" olarak hashleniyor
                });
            }

            context.Customers.AddRange(customers);
            context.SaveChanges();
        }
    }
}
