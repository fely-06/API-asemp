namespace API_asemp.Models.Tablero
{
    /// <summary>
    /// Contiene el mismo resumen que usa el tablero + la lista de XML
    /// para poder exportar a Excel/PDF sin tocar el front.
    /// </summary>
    public class TableroIvaConDetalleDTO
    {
        // Resumen ya existente del tablero
        public TableroIvaDTO Resumen { get; set; } = new();

        // Detalle por cada XML (emitidos/recibidos)
        public List<IvaXmlDetalleDTO> DetalleXml { get; set; } = new();
    }
}
