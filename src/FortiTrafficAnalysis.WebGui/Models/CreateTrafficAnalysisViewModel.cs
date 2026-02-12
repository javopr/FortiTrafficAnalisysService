using System;
using System.ComponentModel.DataAnnotations;

namespace FortiTrafficAnalysis.WebGui.Models
{
    public class CreateTrafficAnalysisViewModel
    {
        [Required(ErrorMessage = "Summary is required")]
        [StringLength(200, ErrorMessage = "Summary cannot exceed 200 characters")]
        [Display(Name = "Summary")]
        public string Summary { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select a FortiGate device")]
        [Display(Name = "FortiGate Device")]
        public Guid FGID { get; set; }
    }
}
