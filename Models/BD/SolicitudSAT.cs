using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_asemp.Models.BD
{
    public class SolicitudSAT
    {
        [Key]
        public int id { get; set; }


        // Relaciones
        [Column("usuario_id")]
        public int usuario_id { get; set; }

        [ForeignKey(nameof(usuario_id))]
        public Usuario? usuario { get; set; }

        [Column("cliente_id")]
        public int cliente_id { get; set; }

        [ForeignKey(nameof(cliente_id))]
        public Cliente? cliente { get; set; }

        [Column("certificado_id")]
        public int certificado_id { get; set; }

        [ForeignKey(nameof(certificado_id))]
        public CertificadoSAT? certificado { get; set; }

        // Datos básicos de la solicitud
        public string? tipo_solicitud { get; set; }  // Ejemplo: "Emitidas", "Recibidas"
        public string? rfc_emisor { get; set; }
        public string? rfc_receptor { get; set; }

        // Fechas
        public DateTime fecha_inicio { get; set; }
        public DateTime fecha_fin { get; set; }
        public DateTime fecha_creacion { get; set; }
        public DateTime? fecha_ultima_verificacion { get; set; }

        // Datos de validación SAT
        public string? token { get; set; }
        public string? codigo_estado { get; set; }
        public int estado_solicitud { get; set; }  // 1=Aceptada, 2=Proceso, 3=Terminada, etc.
        public string? mensaje { get; set; }
    }
}
