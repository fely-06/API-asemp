namespace API_asemp.Models.BD
{
    public class SatJob
    {
        public int Id { get; set; }

        public string TipoSolicitud { get; set; } = null!;
        public DateTime RangoInicio { get; set; }
        public DateTime RangoFin { get; set; }

        public DateTime FechaProgramada { get; set; }
        public int IntervaloVerificacionMin { get; set; }
        public int MaxReintentos { get; set; }

        public string Estado { get; set; } = "Pendiente";
        public string? MensajeError { get; set; }

        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimaEjecucion { get; set; }


        public int UsuarioId { get; set; }

        public List<SatJobCliente> Clientes { get; set; } = new();
        public List<SatJobLog> Logs { get; set; } = new();
    }
}
