namespace API_asemp.Models
{
    public class SolicitudSATFiltro
    {
        // Paginación
        public int pagina { get; set; } = 1;
        public int tamano { get; set; } = 25;

        // Texto general (usuario, cliente, rfc, tipo_solicitud, etc.)
        public string? texto { get; set; }

        // Filtros específicos
        public List<int>? estadoLista { get; set; }        // 1..6
        public string? tipo { get; set; }         // "emitidos" / "recibidos"
        public int? cliente_id { get; set; }
        public int? usuario_id { get; set; }

        // Rango de fechas (del SAT)
        public string? fecha_inicio { get; set; }
        public string? fecha_fin { get; set; }
    }
}
