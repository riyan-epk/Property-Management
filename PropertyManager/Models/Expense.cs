using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManager.Models
{
    public class Expense
    {
        [Key]
        public int Id { get; set; }

        public int AgreementId { get; set; }

        [Required]
        [MaxLength(7)]
        public string Month { get; set; } = string.Empty; // Format: YYYY-MM

        [Column(TypeName = "decimal(18,2)")]
        public decimal Electricity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Maintenance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Other { get; set; }

        [NotMapped]
        public decimal Total => Electricity + Maintenance + Other;

        [ForeignKey(nameof(AgreementId))]
        public Agreement? Agreement { get; set; }
    }
}
