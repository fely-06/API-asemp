namespace API_asemp.Models.Cobros
{
    public class FiltroReporteCobrosDTO
    {
        public int Mes { get; set; }
        public int Anio { get; set; }
        public int ClienteId { get; set; } = 0;   // 0 = todos
        public string Tipo { get; set; } = "PENDIENTES";
        // Tipos: PENDIENTES, ATRASADOS, PAGADOS
    }
}
