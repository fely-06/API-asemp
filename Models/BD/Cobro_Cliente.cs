using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_asemp.Models.BD
{
    [Table("cobros_clientes")]
    public class Cobro_Cliente
    {
        public int id { get; set; }

        // Llave foránea hacia Clientes
        [Column("cliente_id")]
        public int cliente_id { get; set; }

        [ForeignKey(nameof(cliente_id))]
        public Cliente? cliente { get; set; }

        // Campos principales
        public decimal subtotal { get; set; }
        public string? descripcion { get; set; }

        public decimal iva_porcentaje { get; set; }

        // NUEVO: IVA calculado
        public decimal iva_monto { get; set; }

        public decimal total { get; set; }

        // Información temporal
        public int mes { get; set; }
        public int anio { get; set; }

        // Estado del pago
        public string? estado_pago { get; set; }

        // Fechas
        public DateTime fecha_emision { get; set; } = DateTime.UtcNow;
        public DateTime? fecha_pago { get; set; }

        // NUEVO: Folio interno
        public int? folio { get; set; }

        // Documento comprobante (ruta PDF)
        public string? comprobante_pdf { get; set; }

        public decimal isr_ret { get; set; } = 0;
        public decimal iva_ret { get; set; } = 0;

    }
}
