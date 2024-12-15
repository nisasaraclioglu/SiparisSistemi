using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using SiparisSistemi.Helpers;

namespace SiparisSistemi.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customers> Customers { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<Products> Products { get; set; }
        public DbSet<Logs> Logs { get; set; }
        public DbSet<Admin> Admins { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Admin tablosu için tablo adını belirt
            modelBuilder.Entity<Admin>().ToTable("admin");

            // Customers tablosu için konfigürasyon
            modelBuilder.Entity<Customers>(entity =>
            {
                entity.HasKey(e => e.CustomerID);
                entity.Property(e => e.CustomerName).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.CustomerType).IsRequired();
            });

            // Orders tablosu için ilişkiler
            modelBuilder.Entity<Orders>(entity =>
            {
                entity.HasKey(e => e.OrderID);
                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.CustomerID);
                entity.HasOne(d => d.Product)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.ProductID);
            });

            // Logs tablosu için ilişkiler
            modelBuilder.Entity<Logs>(entity =>
            {
                entity.HasKey(e => e.LogID);
                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Logs)
                    .HasForeignKey(d => d.CustomerID);
                entity.HasOne(d => d.Order)
                    .WithMany()
                    .HasForeignKey(d => d.OrderID);
            });

            // Varsayılan admin hesabı
            modelBuilder.Entity<Admin>().HasData(
                new Admin
                {
                    AdminID = 1,
                    Username = "admin",
                    PasswordHash = HashHelper.HashPassword("admin123") // Güvenli bir şifre kullanın
                }
            );
        }
    }
}
