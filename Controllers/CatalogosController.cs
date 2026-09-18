using API_asemp.Datos;
using API_asemp.Models.BD;
using Herramientas.Validaciones;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // ← necesario para [Authorize]

namespace API_asemp.Controllers
{
    // Controlador para manejar los catálogos del sistema.
    // Ruta base: /catalogos
    [ApiController]
    [Route("catalogos")]
    [Authorize] // ← requiere token JWT válido para acceder
    public class CatalogosController : ControllerBase
    {
        private readonly Catalogos _catalogos;

        // Se recibe la clase Catalogos desde la capa de datos
        public CatalogosController(Catalogos catalogosService)
        {
            _catalogos = catalogosService;
        }

        // ===============================================================
        // ========== MÉTODOS DISPONIBLES EN LA API ======================
        // ===============================================================

        /// <summary>
        /// Devuelve todos los catálogos registrados.
        /// </summary>
        [RequireAccion("catalogos.ver")]
        [HttpGet("lista")]
        public IActionResult GetLista()
        {
            try
            {
                var lista = _catalogos.GetLista();
                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Busca un catálogo por su ID.
        /// </summary>
        [RequireAccion("catalogos.ver")]
        [HttpGet("buscar/{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _catalogos.Get(id);
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Agrega un nuevo catálogo.
        /// </summary>
        [RequireAccion("catalogos.crear")]
        [HttpPost("registrar")]
        public IActionResult Registrar([FromBody] Catalogo catalogo)
        {
            try
            {
                Ensure.ValidarNulo(catalogo, "El objeto catálogo no puede ser nulo.");

                var resultado = _catalogos.Registrar(catalogo);
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
        /// Edita un catálogo existente.
        /// </summary>
        [RequireAccion("catalogos.editar")]
        [HttpPut("editar/{id}")]
        public IActionResult Editar(int id, [FromBody] Catalogo catalogo)
        {
            try
            {
                Ensure.ValidarNulo(catalogo, "El objeto catálogo no puede ser nulo.");
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                catalogo.id = id;

                var resultado = _catalogos.Actualizar(catalogo);
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
        /// Elimina un catálogo por su ID.
        /// </summary>
        [RequireAccion("catalogos.eliminar")]
        [HttpDelete("eliminar/{id}")]
        public IActionResult Eliminar(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _catalogos.Eliminar(id);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
