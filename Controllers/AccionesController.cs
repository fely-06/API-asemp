using API_asemp.Datos;
using API_asemp.Models.BD;
using Herramientas.Validaciones;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace API_asemp.Controllers
{
    [ApiController]
    [Route("acciones")]
    [Authorize]
    public class AccionesController : ControllerBase
    {
        private readonly Acciones _acciones;

        public AccionesController(Acciones accionesService)
        {
            _acciones = accionesService;
        }

        // =============================
        // LISTA
        // =============================
        [RequireAccion("acciones.ver")]
        [HttpGet("lista")]
        public IActionResult GetLista()
        {
            try
            {
                var lista = _acciones.GetLista();
                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // =============================
        // BUSCAR POR ID
        // =============================
        [RequireAccion("acciones.ver")]
        [HttpGet("buscar/{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _acciones.Get(id);
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // =============================
        // REGISTRAR
        // =============================
        [RequireAccion("acciones.crear")]
        [HttpPost("registrar")]
        public IActionResult Registrar([FromBody] Accion accion)
        {
            try
            {
                Ensure.ValidarNulo(accion, "El objeto acción no puede ser nulo.");

                var resultado = _acciones.Registrar(accion);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // =============================
        // EDITAR
        // =============================
        [RequireAccion("acciones.editar")]
        [HttpPut("editar/{id}")]
        public IActionResult Editar(int id, [FromBody] Accion accion)
        {
            try
            {
                Ensure.ValidarNulo(accion, "El objeto acción no puede ser nulo.");
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                accion.id = id;

                var resultado = _acciones.Actualizar(accion);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // =============================
        // ELIMINAR
        // =============================
        [RequireAccion("acciones.eliminar")]
        [HttpDelete("eliminar/{id}")]
        public IActionResult Eliminar(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _acciones.Eliminar(id);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // =============================
        // AGRUPADO
        // =============================
        [RequireAccion("acciones.ver")]
        [HttpGet("agrupado")]
        public IActionResult GetAgrupado()
        {
            try
            {
                var resultado = _acciones.GetAgrupado();
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
