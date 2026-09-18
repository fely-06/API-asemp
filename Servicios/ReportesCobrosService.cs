using API_asemp.Contextos;
using API_asemp.Models.Cobros;
using Microsoft.EntityFrameworkCore;

namespace API_asemp.Servicios
{
    public class ReportesCobrosService
    {
        private readonly myDbContext _db;

        public ReportesCobrosService(myDbContext db)
        {
            _db = db;
        }

        public async Task<List<CobroPendienteDTO>> ObtenerReporte(FiltroReporteCobrosDTO filtro)
        {
            var query = _db.cobros_clientes
                .Include(c => c.cliente)
                    .ThenInclude(cli => cli.certificados_sat)
                .AsQueryable();

            // FILTRO MES/AÑO
            query = query.Where(x => x.anio == filtro.Anio && x.mes == filtro.Mes);

            // FILTRO CLIENTE
            if (filtro.ClienteId > 0)
                query = query.Where(x => x.cliente_id == filtro.ClienteId);

            // TIPO DE REPORTE
            switch (filtro.Tipo.ToUpper())
            {
                case "PENDIENTES":
                    query = query.Where(x => x.estado_pago == "PENDIENTE");
                    break;

                case "PAGADOS":
                    query = query.Where(x => x.estado_pago == "PAGADO");
                    break;

                case "ATRASADOS":
                    query = query.Where(x =>
                        x.estado_pago == "PENDIENTE" &&
                        (x.anio < filtro.Anio ||
                         (x.anio == filtro.Anio && x.mes < filtro.Mes)));
                    break;
            }

            // MAPEO DTO
            return await query.Select(x => new CobroPendienteDTO
            {
                ClienteId = x.cliente_id,
                Cliente = x.cliente!.razon_social ?? "",

                RFC = x.cliente!.certificados_sat
                        .Where(cs => cs.estatus == true)
                        .Select(cs => cs.rfc)
                        .FirstOrDefault() ?? "",

                Total = x.total,
                Mes = GetNombreMes(x.mes),
                Anio = x.anio,
                EstadoPago = x.estado_pago,
                Folio = x.folio.HasValue ? x.folio.Value.ToString() : "",
                FechaPago = x.fecha_pago
            }).ToListAsync();
        }

        private static string GetNombreMes(int mes)
        {
            string[] nombres =
            {
                "", "Enero", "Febrero", "Marzo", "Abril",
                "Mayo", "Junio", "Julio", "Agosto",
                "Septiembre", "Octubre", "Noviembre", "Diciembre"
            };

            return (mes >= 1 && mes <= 12) ? nombres[mes] : "";
        }
    }
}
