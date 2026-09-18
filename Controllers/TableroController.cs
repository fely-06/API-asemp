using API_asemp.Models.Tablero;
using API_asemp.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_asemp.Controllers
{
    [ApiController]
    [Route("tablero")]
    [Authorize]
    public class TableroController : ControllerBase
    {
        private readonly ITableroFiscalService _service;

        public TableroController(ITableroFiscalService service)
        {
            _service = service;
        }

        // ============================================================
        // TABLERO IVA (RESUMEN)
        // ============================================================
        // Admin:
        //   GET /tablero/iva?clienteId=5&anio=2024&mes=9
        //
        // Cliente:
        //   GET /tablero/iva?anio=2024&mes=9
        //
        [HttpGet("iva")]
        public ActionResult<TableroIvaDTO> GetIva(
            int? clienteId,
            int anio,
            int mes)
        {
            if (anio <= 0 || mes <= 0)
                return BadRequest("Parámetros inválidos.");

            // Rol del usuario
            var rol = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            int clienteFinalId;

            if (rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                if (!clienteId.HasValue || clienteId <= 0)
                    return BadRequest("Debe seleccionar un cliente.");

                clienteFinalId = clienteId.Value;
            }
            else
            {
                // 🔒 Cliente: se fuerza desde el JWT
                var clienteIdClaim = User.FindFirstValue("cliente_id");

                if (string.IsNullOrWhiteSpace(clienteIdClaim))
                    return Unauthorized("No tienes un cliente asignado.");

                clienteFinalId = int.Parse(clienteIdClaim);
            }

            var dto = _service.GenerarTablero(clienteFinalId, anio, mes);
            return Ok(dto);
        }

        // ============================================================
        // TABLERO IVA CON DETALLE (XML)
        // ============================================================
        // Admin:
        //   GET /tablero/iva/detalle?clienteId=5&anio=2024&mes=9
        //
        // Cliente:
        //   GET /tablero/iva/detalle?anio=2024&mes=9
        //
        [HttpGet("iva/detalle")]
        public ActionResult<TableroIvaConDetalleDTO> GetIvaConDetalle(
            int? clienteId,
            int anio,
            int mes)
        {
            if (anio <= 0 || mes <= 0)
                return BadRequest("Parámetros inválidos.");

            var rol = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            int clienteFinalId;

            if (rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                if (!clienteId.HasValue || clienteId <= 0)
                    return BadRequest("Debe seleccionar un cliente.");

                clienteFinalId = clienteId.Value;
            }
            else
            {
                var clienteIdClaim = User.FindFirstValue("cliente_id");

                if (string.IsNullOrWhiteSpace(clienteIdClaim))
                    return Unauthorized("No tienes un cliente asignado.");

                clienteFinalId = int.Parse(clienteIdClaim);
            }

            var dto = _service.GenerarTableroConDetalle(clienteFinalId, anio, mes);
            return Ok(dto);
        }
    }
}
