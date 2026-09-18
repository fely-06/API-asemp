using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_asemp.Models.BD
{
    [Table("verificacionessat")]
    public class VerificacionesSAT
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("solicitud_id")]
        public int SolicitudId { get; set; }  // ✅ Debe ser int, no long

        [ForeignKey(nameof(SolicitudId))]
        public SolicitudSAT? Solicitud { get; set; }

        [Column("codigo_estado")]
        public string? CodigoEstado { get; set; }

        [Column("estado_solicitud")]
        public short? EstadoSolicitud { get; set; }

        [Column("mensaje")]
        public string? Mensaje { get; set; }

        [Column("numero_cfdis")]
        public int? NumeroCfdis { get; set; }

        [Column("fecha_verificacion")]
        public DateTime FechaVerificacion { get; set; } = DateTime.UtcNow;
    }
}
