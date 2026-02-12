using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FortiTrafficAnalysis.Domain.Entities
{
    /// <summary>
    /// Represents FortiGate traffic log entries for analysis
    /// </summary>
    [Table("TrafficLogs")]
    public class TrafficLog
    {
        [Key]
        public Guid TrafficLogID { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TrafficAnalysisID { get; set; }

        public Guid? FGID { get; set; }

        // Log metadata
        [Column(TypeName = "date")]
        public DateTime? LogDate { get; set; }

        [StringLength(50)]
        public string? LogTime { get; set; }

        [StringLength(50)]
        public string? LogId { get; set; }

        // Source information
        [StringLength(100)]
        public string? SrcIP { get; set; }

        [StringLength(100)]
        public string? SrcInt { get; set; }

        [StringLength(20)]
        public string? SrcPort { get; set; }

        // Destination information
        [StringLength(100)]
        public string? DstIP { get; set; }

        [StringLength(100)]
        public string? DstInt { get; set; }

        [StringLength(20)]
        public string? DstPort { get; set; }

        // Traffic details
        [StringLength(50)]
        public string? Proto { get; set; }

        [StringLength(50)]
        public string? PolicyId { get; set; }

        [StringLength(50)]
        public string? Action { get; set; }

        // Additional fields from FortiGate logs
        [StringLength(100)]
        public string? Service { get; set; }

        [StringLength(50)]
        public string? SessionId { get; set; }

        [StringLength(100)]
        public string? PolicyName { get; set; }

        public long? SentByte { get; set; }

        public long? RcvdByte { get; set; }

        public int? Duration { get; set; }

        // Raw log line (complete original line)
        [Column(TypeName = "nvarchar(max)")]
        public string? RawLogLine { get; set; }

        public DateTime ImportedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(TrafficAnalysisID))]
        public virtual TrafficAnalysis TrafficAnalysis { get; set; }

        [ForeignKey(nameof(FGID))]
        public virtual FortiGate? FortiGate { get; set; }
    }
}

