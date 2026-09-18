using API_asemp.Datos;
using API_asemp.Models.BD;
using Herramientas.Validaciones;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // ← necesario para [Authorize]

namespace API_asemp.Controllers
{
    // Controlador REST para la gestión de departamentos.
    // Ruta base: /departamentos
    [RequireAccion("departamentos.ver")]

    [ApiController]
    [Route("departamentos")]
    [Authorize] // ← requiere token JWT válido para acceder
    public class DepartamentosController : ControllerBase
    {
        private readonly Departamentos _departamentos;

        public DepartamentosController(Departamentos departamentosService)
        {
            _departamentos = departamentosService;
        }

        // ===============================================================
        // ========== MÉTODOS HTTP PARA CONSUMIR LA API ==================
        // ===============================================================

        /// <summary>
        /// Obtiene la lista completa de departamentos registrados.
        /// </summary>
        /// 
        [RequireAccion("departamentos.ver")]
        [HttpGet("lista")]
        public IActionResult GetLista()
        {
            try
            {
                var lista = _departamentos.GetLista();
                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Obtiene la información de un departamento específico.
        /// </summary>
        /// 
        [RequireAccion("departamentos.ver")]
        [HttpGet("buscar/{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID del departamento no es válido.");

                var resultado = _departamentos.Get(id);
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Registra un nuevo departamento.
        /// </summary>
        /// 
        [RequireAccion("departamentos.crear")]
        [HttpPost("registrar")]
        public IActionResult Registrar([FromBody] Departamento departamento)
        {
            try
            {
                Ensure.ValidarNulo(departamento, "El objeto departamento no puede ser nulo.");

                var resultado = _departamentos.Registrar(departamento);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Actualiza un departamento existente.
        /// </summary>
        /// 
        [RequireAccion("departamentos.editar")]
        [HttpPut("editar/{id}")]
        public IActionResult Editar(int id, [FromBody] Departamento departamento)
        {
            try
            {
                Ensure.ValidarNulo(departamento, "El objeto departamento no puede ser nulo.");
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                departamento.id = id;

                var resultado = _departamentos.Actualizar(departamento);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Elimina un departamento por su ID.
        /// </summary>
        /// 

        [RequireAccion("departamentos.eliminar")]
        [HttpDelete("eliminar/{id}")]
        public IActionResult Eliminar(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID del departamento no es válido.");

                var resultado = _departamentos.Eliminar(id);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // Combo simple (id, nombre)
        [RequireAccion("departamentos.ver")]
        [HttpGet("combo")]
        [AllowAnonymous]
        public IActionResult GetCombo()
        {
            try
            {
                var lista = _departamentos.GetLista()
                    .Select(x => new { id = x.id, nombre = x.nombre })
                    .ToList();

                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

    }
}
