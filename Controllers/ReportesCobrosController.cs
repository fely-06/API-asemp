using API_asemp.Models.Cobros;
using API_asemp.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_asemp.Controllers
{
    [ApiController]
    [Route("api/reportes/cobros")]
    [Authorize(Roles = "Administrador,Empleado")]
    [RequireAccion("ccobros.ver")]
    public class ReportesCobrosController : ControllerBase
    {
        private readonly ReportesCobrosService _service;
        private readonly ReportesCobrosArchivoService _archivoService;

        public ReportesCobrosController(
            ReportesCobrosService service,
            ReportesCobrosArchivoService archivoService)
        {
            _service = service;
            _archivoService = archivoService;
        }

        [HttpPost("lista")]
        public async Task<IActionResult> ObtenerReporte([FromBody] FiltroReporteCobrosDTO filtro)
        {
            var datos = await _service.ObtenerReporte(filtro);
            return Ok(datos);
        }

        [HttpPost("excel")]
        public async Task<IActionResult> Excel([FromBody] FiltroReporteCobrosDTO filtro)
        {
            var datos = await _service.ObtenerReporte(filtro);

            var archivo = _archivoService.GenerarExcel(datos);

            return File(
                archivo,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Reporte_Cobros_{filtro.Mes}_{filtro.Anio}.xlsx");
        }

        [HttpPost("pdf/completo")]
        public async Task<IActionResult> PdfCompleto([FromBody] FiltroReporteCobrosDTO filtro)
        {
            var datos = await _service.ObtenerReporte(filtro);

            var archivo = _archivoService.GenerarPdfCompleto(
                datos,
                $"Reporte Completo de Cobros — {filtro.Mes}/{filtro.Anio}");

            return File(
                archivo,
                "application/pdf",
                $"Reporte_Cobros_{filtro.Mes}_{filtro.Anio}.pdf");
        }
    }
}
