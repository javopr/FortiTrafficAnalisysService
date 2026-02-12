using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FortiTrafficAnalysis.Domain.Entities
{
    /// <summary>
    /// Represents a customer/tenant in the multi-tenant application
    /// </summary>
    [Table("Customers")]
    public class Customer
    {
        [Key]
        public Guid CustomerID { get; set; }

        [Required]
        [StringLength(50)]
        public string CustomerName { get; set; }

        // Navigation properties
        public virtual ICollection<FTAService> FTAServices { get; set; }

        public Customer()
        {
            CustomerID = Guid.NewGuid();
            FTAServices = new HashSet<FTAService>();
        }
    }
}
