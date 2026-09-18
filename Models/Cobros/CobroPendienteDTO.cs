namespace API_asemp.Models.Cobros
{
    public class CobroPendienteDTO
    {
        public int ClienteId { get; set; }
        public string Cliente { get; set; }
        public string RFC { get; set; }
        public decimal Total { get; set; }
        public string Mes { get; set; }
        public int Anio { get; set; }
        public string EstadoPago { get; set; }
        public string Folio { get; set; }
        public DateTime? FechaPago { get; set; }
    }
}
