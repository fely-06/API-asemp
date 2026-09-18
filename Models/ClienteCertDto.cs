using Microsoft.AspNetCore.Http;

namespace API_asemp.Models
{
    public class ClienteCertDto
    {
        public int id { get; set; }
        public int usuario_id { get; set; }
        public string? razon_social { get; set; }
        public string? telefono { get; set; }
        public string? correo_electronico { get; set; }
        public string? direccion { get; set; }
        public decimal honorarios_subtotal { get; set; }
        public string? rfc { get; set; }
        public string? contrasena { get; set; }
        public bool estatus { get; set; } = true;

        // 🔹 Archivos opcionales (para registrar o actualizar)
        public IFormFile? cer { get; set; }
        public IFormFile? key { get; set; }

        // 🔹 Nuevas propiedades para vigencia
        public DateTime? fecha_vigencia_inicio { get; set; }
        public DateTime? fecha_vigencia_fin { get; set; }


        // 🔥 NUEVOS CAMPOS PARA RETENCIONES
        public bool usa_retenciones { get; set; } = false;
        public decimal? porc_isr_ret { get; set; } = 0;
        public decimal? porc_iva_ret { get; set; } = 0;
    }
}
