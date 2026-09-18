using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_asemp.Models.BD
{
    public class Cliente
    {
        public int id { get; set; }

        [Column("usuario_id")]
        public int usuario_id { get; set; } // Relación con el usuario que lo administra

        public string? razon_social { get; set; }
        public string? telefono { get; set; }
        public string? correo_electronico { get; set; }
        public string? direccion { get; set; }

        public DateTime fecha_registro { get; set; }
        public decimal honorarios_subtotal { get; set; }
        public bool estatus { get; set; }



        public bool usa_retenciones { get; set; }   // si aplica o no
        public decimal porc_isr_ret { get; set; }   // ej. 0.10
        public decimal porc_iva_ret { get; set; }   // ej. 0.10667



        // Relaciones
        [ForeignKey(nameof(usuario_id))]
        public Usuario? usuario { get; set; }

        // 👇 Agrega esta propiedad de navegación
        public virtual ICollection<CertificadoSAT> certificados_sat { get; set; } = new List<CertificadoSAT>();
    }
}

