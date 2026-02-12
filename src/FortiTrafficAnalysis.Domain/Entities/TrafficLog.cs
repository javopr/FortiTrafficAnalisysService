using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FortiTrafficAnalysis.Domain.Entities
{
    /// <summary>
    /// Represents uploaded FortiGate traffic logs for analysis
    /// </summary>
    [Table("TrafficLogs")]
    public class TrafficLog
    {
        [Key]
        public Guid LogTempID { get; set; }

        [Required]
        public Guid CustomerID { get; set; }

        public Guid? FGID { get; set; } // Nullable - optional link to specific FortiGate

        [Required]
        public DateTime LogTimestamp { get; set; }

        [Required]
        [StringLength(50)]
        public string SourceIP { get; set; }

        [Required]
        [StringLength(50)]
        public string DestinationIP { get; set; }

        [Required]
        [StringLength(10)]
        public string SourcePort { get; set; }

        [Required]
        [StringLength(10)]
        public string DestinationPort { get; set; }

        [Required]
        [StringLength(50)]
        public string Protocol { get; set; }

        [Required]
        [StringLength(50)]
        public string PolicyAction { get; set; } // "accept" or "deny"

        [Column(TypeName = "nvarchar(max)")]
        public string RawLogLine { get; set; }

        // Navigation properties
        [ForeignKey(nameof(CustomerID))]
        public virtual Customer Customer { get; set; }

        [ForeignKey(nameof(FGID))]
        public virtual FortiGate FortiGate { get; set; }

        public TrafficLog()
        {
            LogTempID = Guid.NewGuid();
            LogTimestamp = DateTime.UtcNow;
        }
    }
}
