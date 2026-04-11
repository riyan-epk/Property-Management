using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PropertyManager.Models
{
    public class Tenant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(20)]
        public string CNIC { get; set; } = string.Empty;

        public ICollection<Agreement> Agreements { get; set; } = new List<Agreement>();
    }
}
