using Microsoft.EntityFrameworkCore;
using PropertyManager.Models;
using System;
using System.IO;

namespace PropertyManager.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Property> Properties { get; set; } = null!;
        public DbSet<Unit> Units { get; set; } = null!;
        public DbSet<Tenant> Tenants { get; set; } = null!;
        public DbSet<Agreement> Agreements { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;

        private static string DbPath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "PropertyManager.db");

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={DbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Indexes ──
            modelBuilder.Entity<Unit>()
                .HasIndex(u => new { u.PropertyId, u.UnitNumber }).IsUnique();

            modelBuilder.Entity<Expense>()
                .HasIndex(e => new { e.AgreementId, e.Month }).IsUnique();

            // ── Relationships ──
            modelBuilder.Entity<Unit>()
                .HasOne(u => u.Property)
                .WithMany(p => p.Units)
                .HasForeignKey(u => u.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Agreement>()
                .HasOne(a => a.Tenant)
                .WithMany(t => t.Agreements)
                .HasForeignKey(a => a.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Agreement>()
                .HasOne(a => a.Unit)
                .WithMany(u => u.Agreements)
                .HasForeignKey(a => a.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Agreement)
                .WithMany(a => a.Expenses)
                .HasForeignKey(e => e.AgreementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Agreement)
                .WithMany(a => a.Payments)
                .HasForeignKey(p => p.AgreementId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Seed Data ──
            //SeedData(modelBuilder);
        }

        //private void SeedData(ModelBuilder modelBuilder)
        //{
        //    // Properties
        //    modelBuilder.Entity<Property>().HasData(
        //        new Property { Id = 1, Name = "Sunrise Plaza", Location = "Main Boulevard, Lahore", Type = "Commercial" },
        //        new Property { Id = 2, Name = "Green Valley Apartments", Location = "DHA Phase 5, Karachi", Type = "Residential" }
        //    );

        //    // Units
        //    modelBuilder.Entity<Unit>().HasData(
        //        new Unit { Id = 1, PropertyId = 1, UnitNumber = "S-101", BaseRent = 25000, Status = "Occupied" },
        //        new Unit { Id = 2, PropertyId = 1, UnitNumber = "S-102", BaseRent = 30000, Status = "Vacant" },
        //        new Unit { Id = 3, PropertyId = 2, UnitNumber = "A-201", BaseRent = 18000, Status = "Occupied" },
        //        new Unit { Id = 4, PropertyId = 2, UnitNumber = "A-202", BaseRent = 20000, Status = "Vacant" }
        //    );

        //    // Tenants
        //    modelBuilder.Entity<Tenant>().HasData(
        //        new Tenant { Id = 1, Name = "Ahmed Ali", Phone = "0300-1234567", CNIC = "35201-1234567-1" },
        //        new Tenant { Id = 2, Name = "Sara Khan", Phone = "0321-9876543", CNIC = "42101-9876543-2" }
        //    );

        //    // Agreements with per-agreement rent increase settings
        //    modelBuilder.Entity<Agreement>().HasData(
        //        new Agreement
        //        {
        //            Id = 1,
        //            TenantId = 1,
        //            UnitId = 1,
        //            StartDate = new DateTime(2025, 1, 1),
        //            EndDate = new DateTime(2027, 12, 31),
        //            BaseRent = 25000,
        //            IncreaseType = IncreaseType.Yearly,
        //            IncreaseAfterMonths = 12,
        //            IncreasePercentage = 10
        //        },
        //        new Agreement
        //        {
        //            Id = 2,
        //            TenantId = 2,
        //            UnitId = 3,
        //            StartDate = new DateTime(2025, 6, 1),
        //            EndDate = new DateTime(2026, 5, 31),
        //            BaseRent = 18000,
        //            IncreaseType = IncreaseType.CustomMonths,
        //            IncreaseAfterMonths = 6,
        //            IncreasePercentage = 5
        //        }
        //    );

        //    // Expenses
        //    modelBuilder.Entity<Expense>().HasData(
        //        new Expense { Id = 1, AgreementId = 1, Month = "2025-01", Electricity = 3000, Maintenance = 1000, Other = 500 },
        //        new Expense { Id = 2, AgreementId = 1, Month = "2025-02", Electricity = 3200, Maintenance = 1000, Other = 0 },
        //        new Expense { Id = 3, AgreementId = 2, Month = "2025-06", Electricity = 2500, Maintenance = 800, Other = 200 }
        //    );

        //    // Payments
        //    modelBuilder.Entity<Payment>().HasData(
        //        new Payment { Id = 1, AgreementId = 1, Month = "2025-01", PaidAmount = 29500, PaymentDate = new DateTime(2025, 1, 5) },
        //        new Payment { Id = 2, AgreementId = 1, Month = "2025-02", PaidAmount = 25000, PaymentDate = new DateTime(2025, 2, 8) },
        //        new Payment { Id = 3, AgreementId = 2, Month = "2025-06", PaidAmount = 21500, PaymentDate = new DateTime(2025, 6, 3) }
        //    );
        //}

 
        public static void Initialize()
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();
        }
    }
}
