namespace API_asemp.Models.SAT
{
    public class SatJobResponseDTO
    {
        public int Id { get; set; }
        public string TipoSolicitud { get; set; } = null!;
        public DateTime RangoInicio { get; set; }
        public DateTime RangoFin { get; set; }
        public DateTime FechaProgramada { get; set; }
        public int IntervaloVerificacionMin { get; set; }
        public int MaxReintentos { get; set; }
        public string Estado { get; set; } = null!;
    }
}
