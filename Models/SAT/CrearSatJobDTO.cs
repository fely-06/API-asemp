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

        // Clientes cuya descarga se hace UNA vez, con el rango RangoInicio..RangoFin
        public List<int> ClientesIds { get; set; } = new();

        // Clientes cuya descarga se repite TODOS LOS DÍAS (siempre baja los CFDI del día anterior).
        // Si un cliente viene en ambas listas, prevalece la diaria.
        public List<int> ClientesDiariosIds { get; set; } = new();

        // Separación entre clientes (mínimo 5 min)
        public int IntervaloEntreClientesMin { get; set; } = 5;


        // opcional: lo usamos solo internamente
        public int UsuarioId { get; set; }
    }
}
