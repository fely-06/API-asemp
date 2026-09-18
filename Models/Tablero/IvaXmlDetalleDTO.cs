namespace API_asemp.Models.Tablero
{
    // Detalle por cada XML
    public class IvaXmlDetalleDTO
    {
        public string TipoMovimiento { get; set; } = string.Empty; // Emitido / Recibido
        public string RutaArchivo { get; set; } = string.Empty;
        public string NombreXml { get; set; } = string.Empty;

        public string Uuid { get; set; } = string.Empty;
        public DateTime? Fecha { get; set; }

        public string TipoCfdi { get; set; } = string.Empty;   // I, E, P, N, etc.
        public string MetodoPago { get; set; } = string.Empty; // PUE, PPD, etc.
        public string RfcEmisor { get; set; } = string.Empty;
        public string RfcReceptor { get; set; } = string.Empty;

        public decimal IvaTrasladado { get; set; }
        public decimal IvaRetenido { get; set; }

        public bool IncluidoEnCalculo { get; set; }
        public string MotivoExclusion { get; set; } = string.Empty;
    }
}
