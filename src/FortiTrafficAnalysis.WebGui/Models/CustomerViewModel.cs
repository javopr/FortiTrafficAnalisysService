using System;
using System.ComponentModel.DataAnnotations;

namespace FortiTrafficAnalysis.WebGui.Models
{
    public class CustomerViewModel
    {
        public Guid? CustomerID { get; set; }

        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(50, ErrorMessage = "Customer name cannot exceed 50 characters")]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }

        [Display(Name = "Number of Services")]
        public int ServiceCount { get; set; }

        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; }
    }
}
