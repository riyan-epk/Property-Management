using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManager.Models
{
    /// <summary>
    /// Represents a rental agreement between a tenant and a property unit.
    /// Rent increase settings are stored PER AGREEMENT — not globally.
    /// </summary>
    public class Agreement
    {
        [Key]
        public int Id { get; set; }

        public int TenantId { get; set; }
        public int UnitId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseRent { get; set; }

        // ── Rent Increase Settings (per agreement) ──
        public IncreaseType IncreaseType { get; set; } = IncreaseType.Yearly;
        public int IncreaseAfterMonths { get; set; } = 12;
        public double IncreasePercentage { get; set; } = 10;

        // ── Navigation Properties ──
        [ForeignKey(nameof(TenantId))]
        public Tenant? Tenant { get; set; }

        [ForeignKey(nameof(UnitId))]
        public Unit? Unit { get; set; }

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
