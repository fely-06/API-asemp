using API_asemp.Models.Tablero;

namespace API_asemp.Servicios
{
    public interface ITableroFiscalService
    {
        // Método que ya usas actualmente en el frontend del tablero.
        TableroIvaDTO GenerarTablero(int clienteId, int anio, int mes);

        // NUEVO: para exportación (devuelve resumen + lista de XML).
        TableroIvaConDetalleDTO GenerarTableroConDetalle(int clienteId, int anio, int mes);

        // Más adelante aquí podemos agregar:
        // byte[] GenerarExcelIva(...);
        // byte[] GenerarPdfIva(...);
    }
}
