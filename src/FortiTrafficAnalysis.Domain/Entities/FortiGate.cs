using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace FortiTrafficAnalysis.Domain.Entities
{
    /// <summary>
    /// Represents a FortiGate firewall device
    /// </summary>
    [Table("FortiGates")]
    public class FortiGate
    {
        [Key]
        public Guid FGID { get; set; }

        [Required]
        public Guid FTAID { get; set; }

        [Required]
        [StringLength(50)]
        public string FGHostname { get; set; }

        [Required]
        [StringLength(255)]
        public string FGHost { get; set; } // FQDN or IP Address

        [Required]
        [StringLength(255)]
        public string FGSerial { get; set; }

        [Required]
        [StringLength(255)]
        public string FGvDOM { get; set; }

        [Required]
        [StringLength(255)]
        public string FGapiKey { get; set; }

        [Required]
        [StringLength(50)]
        public string FGStatus { get; set; } // "Active" or "Inactive"

        [Column(TypeName = "varbinary(max)")]
        public byte[]? ConfigFileCompressed { get; set; } // Compressed FortiGate configuration

        public DateTime? ConfigUploadedDate { get; set; }

        // Helper property to work with config as string
        [NotMapped]
        public string? ConfigFile
        {
            get
            {
                if (ConfigFileCompressed == null || ConfigFileCompressed.Length == 0)
                    return null;

                try
                {
                    using (var memoryStream = new MemoryStream(ConfigFileCompressed))
                    using (var gzipStream = new System.IO.Compression.GZipStream(memoryStream, System.IO.Compression.CompressionMode.Decompress))
                    using (var reader = new StreamReader(gzipStream, System.Text.Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    ConfigFileCompressed = null;
                    return;
                }

                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new System.IO.Compression.GZipStream(memoryStream, System.IO.Compression.CompressionLevel.Optimal))
                    using (var writer = new StreamWriter(gzipStream, System.Text.Encoding.UTF8))
                    {
                        writer.Write(value);
                    }
                    ConfigFileCompressed = memoryStream.ToArray();
                }
            }
        }

        // Navigation properties
        [ForeignKey(nameof(FTAID))]
        public virtual FTAService FTAService { get; set; }

        public virtual ICollection<TrafficLog> TrafficLogs { get; set; }

        public FortiGate()
        {
            FGID = Guid.NewGuid();
            TrafficLogs = new HashSet<TrafficLog>();
            FGStatus = "Active";
        }
    }
}
