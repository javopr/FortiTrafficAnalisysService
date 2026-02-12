using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FortiTrafficAnalysis.Domain.Entities
{
    /// <summary>
    /// Represents application users with local or Azure AD authentication
    /// </summary>
    [Table("AppUsers")]
    public class AppUser
    {
        [Key]
        public Guid AppAccessID { get; set; }

        [Required]
        [StringLength(255)]
        public string UserUPN { get; set; }

        [Required]
        public Guid AppGroupID { get; set; }

        [Required]
        [StringLength(255)]
        public string AppUserName { get; set; }

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string AppUserEmail { get; set; }

        // Local authentication support (temporary - for development)
        [StringLength(500)]
        public string? PasswordHash { get; set; }

        // Navigation properties
        [ForeignKey(nameof(AppGroupID))]
        public virtual AppGroup AppGroup { get; set; }

        public AppUser()
        {
            AppAccessID = Guid.NewGuid();
        }
    }
}
