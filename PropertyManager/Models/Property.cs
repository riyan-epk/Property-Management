using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PropertyManager.Models
{
    public class Property
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Type { get; set; } = string.Empty; // Residential, Commercial, etc.

        public ICollection<Unit> Units { get; set; } = new List<Unit>();
    }
}
