namespace API_asemp.Models.SAT
{
    public class CrearSatJobDTO
    {
        public string TipoSolicitud { get; set; } = null!;
        public DateTime RangoInicio { get; set; }
        public DateTime RangoFin { get; set; }

        public DateTime FechaProgramada { get; set; }

        public int IntervaloVerificacionMin { get; set; } = 10;
        public int MaxReintentos { get; set; } = 3;

        public List<int> ClientesIds { get; set; } = new();


        // opcional: lo usamos solo internamente
        public int UsuarioId { get; set; }
    }
}
