using System;
using System.ComponentModel.DataAnnotations;

namespace FortiTrafficAnalysis.WebGui.Models
{
    public class FortiGateViewModel
    {
        public Guid? FGID { get; set; }

        [Required(ErrorMessage = "FTA Service is required")]
        [Display(Name = "FTA Service")]
        public Guid FTAID { get; set; }

        [Display(Name = "Service Job ID")]
        public string? ServiceJobID { get; set; }

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }

        [Required(ErrorMessage = "FG Name is required")]
        [StringLength(50, ErrorMessage = "FG Name cannot exceed 50 characters")]
        [Display(Name = "FG Name")]
        public string FGHostname { get; set; }

        [Required(ErrorMessage = "IP Address/Host is required")]
        [StringLength(255, ErrorMessage = "Host cannot exceed 255 characters")]
        [Display(Name = "IP Address / FQDN")]
        public string FGHost { get; set; }

        [Required(ErrorMessage = "Serial Number is required")]
        [StringLength(255, ErrorMessage = "Serial number cannot exceed 255 characters")]
        [Display(Name = "Serial Number")]
        public string FGSerial { get; set; }

        [Required(ErrorMessage = "vDOM is required")]
        [StringLength(255, ErrorMessage = "vDOM cannot exceed 255 characters")]
        [Display(Name = "Virtual Domain (vDOM)")]
        public string FGvDOM { get; set; }

        [StringLength(255, ErrorMessage = "API Key cannot exceed 255 characters")]
        [Display(Name = "API Key")]
        [DataType(DataType.Password)]
        public string? FGapiKey { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        [Display(Name = "Device Status")]
        public string FGStatus { get; set; }

        [Display(Name = "Number of Logs")]
        public int LogCount { get; set; }
    }

    public class CreateFortiGateViewModel : FortiGateViewModel
    {
        [Required(ErrorMessage = "API Key is required")]
        [StringLength(255, ErrorMessage = "API Key cannot exceed 255 characters")]
        [Display(Name = "API Key")]
        [DataType(DataType.Password)]
        public new string FGapiKey { get; set; }
    }
}

