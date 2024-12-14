using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}
