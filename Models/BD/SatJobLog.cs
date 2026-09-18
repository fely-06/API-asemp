namespace API_asemp.Models.BD
{
    public class SatJobLog
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int? ClienteId { get; set; }

        public DateTime Fecha { get; set; }
        public string Accion { get; set; } = null!;
        public string? Detalle { get; set; }

        public SatJob Job { get; set; } = null!;
        public Cliente? Cliente { get; set; }
    }
}
