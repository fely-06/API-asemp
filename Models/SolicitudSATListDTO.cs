public class SolicitudSATListDTO
{
    public int id { get; set; }

    public int usuario_id { get; set; }
    public string? usuario { get; set; }                 // nombre completo

    public int cliente_id { get; set; }
    public string? cliente { get; set; }                 // razón social

    public int? certificado_id { get; set; }

    public string? tipo_solicitud { get; set; }
    public string? rfc_emisor { get; set; }
    public string? rfc_receptor { get; set; }

    public DateTime fecha_inicio { get; set; }
    public DateTime fecha_fin { get; set; }
    public DateTime fecha_creacion { get; set; }
    public DateTime? fecha_ultima_verificacion { get; set; }

    public string? token { get; set; }

    public string? codigo_estado { get; set; }           // valor real del SAT (5000, 5004, etc.)
    public int estado_solicitud { get; set; }            // valor real del SAT (1..6)

    public string? mensaje { get; set; }

    // ============================================
    // 🔹 NUEVAS PROPIEDADES (TRADUCIDAS)
    // ============================================

    // Texto legible del estado_solicitud
    public string? estado_solicitud_texto { get; set; }

    // Texto legible del codigo_estado del SAT
    public string? codigo_estado_texto { get; set; }
}
