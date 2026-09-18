using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; // 👈 IMPORTANTE para [JsonIgnore]


namespace API_asemp.Models.BD
{
    public class CertificadoSAT
    {
        public int id { get; set; }

        // Relación con el cliente propietario del certificado
        [Column("cliente_id")]
        public int cliente_id { get; set; }

        [ForeignKey(nameof(cliente_id))]
        [JsonIgnore] // 👈 evita el bucle circular de serialización
        public Cliente? cliente { get; set; }

        // Nuevo campo RFC (extraído del .cer)
        public string? rfc { get; set; }

        // Archivos del certificado
        public string? archivo_pfx { get; set; }  // Ruta o nombre del archivo .pfx
        public string? archivo_cer { get; set; }  // Ruta o nombre del archivo .cer
        public string? archivo_key { get; set; }  // Ruta o nombre del archivo .key
        public string? contrasena { get; set; }   // Contraseña del .pfx o .key

        // Fechas de vigencia
        public DateTime? fecha_vigencia_inicio { get; set; }
        public DateTime? fecha_vigencia_fin { get; set; }

        // Estado del certificado
        public bool estatus { get; set; }

        // Fecha en que se registró
        public DateTime fecha_registro { get; set; }
    }
}
