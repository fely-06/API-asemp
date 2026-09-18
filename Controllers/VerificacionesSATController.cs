using API_asemp.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_asemp.Controllers
{
    [RequireAccion("sat.ver")]

    [ApiController]
    [Route("solicitudessat")]
    [Authorize]
    public class VerificacionesSATController : ControllerBase
    {
        private readonly VerificacionesSAT_Datos _datos;

        public VerificacionesSATController(VerificacionesSAT_Datos datos)
        {
            _datos = datos;
        }

        [HttpGet("{id}/verificaciones")]
        public IActionResult GetVerificaciones(int id)
        {
            var lista = _datos.ObtenerHistorial(id);
            return Ok(lista);
        }
    }
}
