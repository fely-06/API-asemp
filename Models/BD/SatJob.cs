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

        // "Unica" = se ejecuta una sola vez | "Diaria" = al terminar se reprograma para el día siguiente
        public string Recurrencia { get; set; } = "Unica";

        // Separación (en minutos) entre la solicitud de un cliente y la del siguiente (orden alfabético)
        public int IntervaloEntreClientesMin { get; set; } = 5;

        public string Estado { get; set; } = "Pendiente";
        public string? MensajeError { get; set; }

        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimaEjecucion { get; set; }


        public int UsuarioId { get; set; }

        public List<SatJobCliente> Clientes { get; set; } = new();
        public List<SatJobLog> Logs { get; set; } = new();
    }
}
