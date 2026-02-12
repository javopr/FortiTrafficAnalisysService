using System;
using System.ComponentModel.DataAnnotations;

namespace FortiTrafficAnalysis.WebGui.Models
{
    public class EditTrafficAnalysisViewModel
    {
        public Guid TrafficAnalysisID { get; set; }

        public string? TicketNumber { get; set; }

        [Required(ErrorMessage = "Summary is required")]
        [StringLength(200, ErrorMessage = "Summary cannot exceed 200 characters")]
        [Display(Name = "Summary")]
        public string Summary { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Read-only display fields - nullable to avoid validation issues
        public string? CustomerName { get; set; }
        public string? ServiceJobID { get; set; }
        public string? FortiGateHostname { get; set; }
        public string? Status { get; set; }
        public string? CreatedByUPN { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}

