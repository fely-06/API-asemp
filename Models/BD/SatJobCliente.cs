namespace API_asemp.Models.BD
{
    public class SatJobCliente
    {
        public int Id { get; set; }

        public int JobId { get; set; }
        public int ClienteId { get; set; }
        public int? SolicitudSatId { get; set; }

        // Hora (UTC) a partir de la cual se puede enviar la solicitud de este cliente.
        // Escalonada: FechaProgramada del job + (posición alfabética * IntervaloEntreClientesMin)
        public DateTime? FechaEnvioProgramada { get; set; }

        public string Estado { get; set; } = "Pendiente";
        public int Intentos { get; set; } = 0;
        public string? MensajeError { get; set; }

        public SatJob Job { get; set; } = null!;
        public Cliente Cliente { get; set; } = null!;
        public SolicitudSAT? SolicitudSAT { get; set; }
    }
}
