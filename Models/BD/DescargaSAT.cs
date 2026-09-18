using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_asemp.Models.BD
{
    public class DescargaSAT
    {
        public int id { get; set; }

        // Relación con la solicitud que originó la descarga
        [Column("solicitud_id")]
        public int solicitud_id { get; set; }

        [ForeignKey(nameof(solicitud_id))]
        public SolicitudSAT? solicitud { get; set; }

        // Información del archivo descargado
        public string? archivo_zip { get; set; }           // Ruta o nombre del archivo ZIP
        public string? carpeta_extraccion { get; set; }    // Carpeta donde se extrajo
        public DateTime fecha_descarga { get; set; }       // Fecha en que se realizó la descarga
        public string? estatus { get; set; }               // Ejemplo: "Completada", "Error", etc.
    }
}
