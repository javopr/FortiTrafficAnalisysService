using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FortiTrafficAnalysis.Domain.Entities
{
    /// <summary>
    /// Represents a firewall policy recommendation derived from traffic log analysis
    /// </summary>
    [Table("TrafficAnalysisRecommendations")]
    public class TrafficAnalysisRecommendation
    {
        [Key]
        public Guid TrafficAnalysisRecommendationID { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TrafficAnalysisID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string RecommendationText { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? AnalysisDetails { get; set; }

        [StringLength(255)]
        public string CreatedByUPN { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey(nameof(TrafficAnalysisID))]
        public virtual TrafficAnalysis TrafficAnalysis { get; set; }
    }
}
