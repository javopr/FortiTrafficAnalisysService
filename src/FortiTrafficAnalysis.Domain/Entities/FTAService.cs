using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FortiTrafficAnalysis.Domain.Entities
{
    /// <summary>
    /// Represents a FortiGate Traffic Analysis service contract for a customer
    /// </summary>
    [Table("FTAServices")]
    public class FTAService
    {
        [Key]
        public Guid FTAID { get; set; }

        [Required]
        [StringLength(50)]
        public string JobID { get; set; }

        [Required]
        public Guid CustomerID { get; set; }

        [Required]
        public DateTime ServiceStart { get; set; }

        [Required]
        public DateTime ServiceEnd { get; set; }

        [Required]
        [StringLength(50)]
        public string ServiceStatus { get; set; } // "Active" or "Inactive"

        // Navigation properties
        [ForeignKey(nameof(CustomerID))]
        public virtual Customer Customer { get; set; }

        public virtual ICollection<FortiGate> FortiGates { get; set; }

        public FTAService()
        {
            FTAID = Guid.NewGuid();
            FortiGates = new HashSet<FortiGate>();
            ServiceStatus = "Active";
        }
    }
}
