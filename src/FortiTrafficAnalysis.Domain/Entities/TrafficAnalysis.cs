using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FortiTrafficAnalysis.Domain.Entities
{
    /// <summary>
    /// Represents a traffic analysis ticket/session
    /// </summary>
    [Table("TrafficAnalysis")]
    public class TrafficAnalysis
    {
        [Key]
        public Guid TrafficAnalysisID { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(10)]
        public string TicketNumber { get; set; }

        [Required]
        [StringLength(200)]
        public string Summary { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        public Guid FGID { get; set; }

        public Guid? CustomerID { get; set; }

        public Guid? FTAID { get; set; }

        [Required]
        [StringLength(255)]
        public string CreatedByUPN { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Open";

        // Navigation properties
        [ForeignKey("FGID")]
        public virtual FortiGate FortiGate { get; set; }

        [ForeignKey("CustomerID")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("FTAID")]
        public virtual FTAService? FTAService { get; set; }

        public virtual ICollection<TrafficLog> TrafficLogs { get; set; } = new List<TrafficLog>();

        public virtual ICollection<TrafficAnalysisRecommendation> Recommendations { get; set; } = new List<TrafficAnalysisRecommendation>();
    }
}
