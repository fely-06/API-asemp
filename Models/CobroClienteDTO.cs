namespace API_asemp.Models;

public class CobroClienteDTO
{
    public int id { get; set; }
    public int? folio { get; set; }       // ← AGREGADO
    public string folio_formato => folio.HasValue
       ? folio.Value.ToString("D5")   // ← 5 dígitos: 00014
       : "-";
    public int cliente_id { get; set; }
    public string cliente { get; set; } = "";

    public decimal subtotal { get; set; }
    public decimal iva { get; set; }
    public decimal isr_ret { get; set; }
    public decimal iva_ret { get; set; }
    public decimal total { get; set; }

    public int mes { get; set; }
    public int anio { get; set; }

    public string estado_pago { get; set; } = "";

    public string? comprobante_pdf { get; set; }   // ← NUEVO
}
