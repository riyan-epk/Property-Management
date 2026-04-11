using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManager.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public int AgreementId { get; set; }

        [Required]
        [MaxLength(7)]
        public string Month { get; set; } = string.Empty; // Format: YYYY-MM

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [ForeignKey(nameof(AgreementId))]
        public Agreement? Agreement { get; set; }
    }
}
