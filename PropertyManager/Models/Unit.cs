using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManager.Models
{
    public class Unit
    {
        [Key]
        public int Id { get; set; }

        public int PropertyId { get; set; }

        [Required]
        [MaxLength(50)]
        public string UnitNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseRent { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Vacant"; // Vacant, Occupied

        [ForeignKey(nameof(PropertyId))]
        public Property? Property { get; set; }

        public ICollection<Agreement> Agreements { get; set; } = new List<Agreement>();
    }
}
