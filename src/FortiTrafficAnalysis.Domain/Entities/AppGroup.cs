using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FortiTrafficAnalysis.Domain.Entities
{
    /// <summary>
    /// Represents application groups/roles (Users, Admins)
    /// </summary>
    [Table("AppGroups")]
    public class AppGroup
    {
        [Key]
        public Guid AppGroupID { get; set; }

        [Required]
        [StringLength(50)]
        public string AppGroupName { get; set; }

        // Navigation properties
        public virtual ICollection<AppUser> AppUsers { get; set; }

        public AppGroup()
        {
            AppGroupID = Guid.NewGuid();
            AppUsers = new HashSet<AppUser>();
        }
    }
}
