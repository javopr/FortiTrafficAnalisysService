using System;
using System.ComponentModel.DataAnnotations;

namespace FortiTrafficAnalysis.WebGui.Models
{
    public class FTAServiceViewModel
    {
        public Guid? FTAID { get; set; }

        [Required(ErrorMessage = "Job ID is required")]
        [StringLength(50, ErrorMessage = "Job ID cannot exceed 50 characters")]
        [Display(Name = "Job ID")]
        public string JobID { get; set; }

        [Required(ErrorMessage = "Customer is required")]
        [Display(Name = "Customer")]
        public Guid CustomerID { get; set; }

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }

        [Required(ErrorMessage = "Service start date is required")]
        [Display(Name = "Service Start Date")]
        [DataType(DataType.Date)]
        public DateTime ServiceStart { get; set; }

        [Required(ErrorMessage = "Service end date is required")]
        [Display(Name = "Service End Date")]
        [DataType(DataType.Date)]
        public DateTime ServiceEnd { get; set; }

        [Required(ErrorMessage = "Service status is required")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        [Display(Name = "Service Status")]
        public string ServiceStatus { get; set; }

        [Display(Name = "Number of Devices")]
        public int DeviceCount { get; set; }
    }
}
