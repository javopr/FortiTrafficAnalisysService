using System;
using System.ComponentModel.DataAnnotations;

namespace FortiTrafficAnalysis.WebGui.Models
{
    public class AppUserViewModel
    {
        public Guid? AppAccessID { get; set; }

        [Required(ErrorMessage = "Username (UPN) is required")]
        [StringLength(255, ErrorMessage = "Username cannot exceed 255 characters")]
        [Display(Name = "Username (UPN)")]
        public string UserUPN { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(255, ErrorMessage = "Full name cannot exceed 255 characters")]
        [Display(Name = "Full Name")]
        public string AppUserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string AppUserEmail { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "User Role")]
        public Guid AppGroupID { get; set; }

        [Display(Name = "Role Name")]
        public string? AppGroupName { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and confirmation do not match")]
        public string? ConfirmPassword { get; set; }
    }

    public class CreateAppUserViewModel : AppUserViewModel
    {
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public new string Password { get; set; }

        [Required(ErrorMessage = "Password confirmation is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and confirmation do not match")]
        public new string ConfirmPassword { get; set; }
    }
}
