using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FortiTrafficAnalysis.Domain.Entities
{
    /// <summary>
    /// Represents a FortiGate firewall device
    /// </summary>
    [Table("FortiGates")]
    public class FortiGate
    {
        [Key]
        public Guid FGID { get; set; }

        [Required]
        public Guid FTAID { get; set; }

        [Required]
        [StringLength(50)]
        public string FGHostname { get; set; }

        [Required]
        [StringLength(255)]
        public string FGHost { get; set; } // FQDN or IP Address

        [Required]
        [StringLength(255)]
        public string FGSerial { get; set; }

        [Required]
        [StringLength(255)]
        public string FGvDOM { get; set; }

        [Required]
        [StringLength(255)]
        public string FGapiKey { get; set; }

        [Required]
        [StringLength(50)]
        public string FGStatus { get; set; } // "Active" or "Inactive"

        // Navigation properties
        [ForeignKey(nameof(FTAID))]
        public virtual FTAService FTAService { get; set; }

        public virtual ICollection<TrafficLog> TrafficLogs { get; set; }

        public FortiGate()
        {
            FGID = Guid.NewGuid();
            TrafficLogs = new HashSet<TrafficLog>();
            FGStatus = "Active";
        }
    }
}
