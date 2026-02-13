# Alternativa: Usar VARBINARY con Compresión

Si los archivos de configuración son muy grandes (>5MB), puedes usar compresión:

## 1. Crear nueva Migration

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

public partial class ChangeConfigFileToCompressed : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add new compressed column
        migrationBuilder.AddColumn<byte[]>(
            name: "ConfigFileCompressed",
            table: "FortiGates",
            type: "varbinary(max)",
            nullable: true);

        // Drop old column (optional - if you want to migrate existing data, do it first)
        // migrationBuilder.DropColumn(name: "ConfigFile", table: "FortiGates");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ConfigFileCompressed", table: "FortiGates");
    }
}
```

## 2. Modificar Entidad FortiGate

```csharp
public class FortiGate
{
    // ...existing properties...
    
    [Column(TypeName = "varbinary(max)")]
    public byte[]? ConfigFileCompressed { get; set; }
    
    public DateTime? ConfigUploadedDate { get; set; }
    
    // Helper property to get decompressed config
    [NotMapped]
    public string? ConfigFile
    {
        get
        {
            if (ConfigFileCompressed == null) return null;
            
            using (var memoryStream = new MemoryStream(ConfigFileCompressed))
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
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
                using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
                using (var writer = new StreamWriter(gzipStream, Encoding.UTF8))
                {
                    writer.Write(value);
                }
                ConfigFileCompressed = memoryStream.ToArray();
            }
        }
    }
}
```

## 3. Agregar usings necesarios

```csharp
using System.IO.Compression;
using System.Text;
```

## Beneficios
- Archivos de 10MB se comprimen a ~1-2MB
- Mejor performance de base de datos
- Menor uso de memoria

## Desventajas
- Requiere CPU para comprimir/descomprimir
- No se puede buscar directamente en SQL
