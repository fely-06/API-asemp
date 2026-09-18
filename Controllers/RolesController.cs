using API_asemp.Datos;
using API_asemp.Models.BD;
using Herramientas.Validaciones;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // ← necesario para [Authorize]

namespace API_asemp.Controllers
{
    // Controlador para manejar los roles del sistema.
    // Ruta base: /roles
    [RequireAccion("roles.ver")]

    [ApiController]
    [Route("roles")]
    [Authorize] // ← requiere token JWT válido para acceder
    public class RolesController : ControllerBase
    {
        private readonly Roles _roles;

        // Se recibe la clase Roles desde la capa de datos
        public RolesController(Roles rolesService)
        {
            _roles = rolesService;
        }

        // ===============================================================
        // ========== MÉTODOS DISPONIBLES EN LA API ======================
        // ===============================================================

        /// <summary>
        /// Devuelve todos los roles registrados.
        /// </summary>
        /// 
        [RequireAccion("roles.ver")]
        [HttpGet("lista")]
        public IActionResult GetLista()
        {
            try
            {
                var lista = _roles.GetLista();
                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Busca un rol por su ID.
        /// </summary>
        /// 
        [RequireAccion("roles.ver")]
        [HttpGet("buscar/{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _roles.Get(id);
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Registra un nuevo rol.
        /// </summary>
        /// 
        [RequireAccion("roles.crear")]
        [HttpPost("registrar")]
        public IActionResult Registrar([FromBody] Rol rol)
        {
            try
            {
                Ensure.ValidarNulo(rol, "El objeto rol no puede ser nulo.");

                var resultado = _roles.Registrar(rol);
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
        /// Edita un rol existente.
        /// </summary>
        /// 
        [RequireAccion("roles.editar")]
        [HttpPut("editar/{id}")]
        public IActionResult Editar(int id, [FromBody] Rol rol)
        {
            try
            {
                Ensure.ValidarNulo(rol, "El objeto rol no puede ser nulo.");
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                rol.id = id;

                var resultado = _roles.Actualizar(rol);
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
        /// Elimina un rol por su ID.
        /// </summary>
        /// 
        [RequireAccion("roles.eliminar")]
        [HttpDelete("eliminar/{id}")]
        public IActionResult Eliminar(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _roles.Eliminar(id);
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
        /// Devuelve solo los roles activos (id y nombre) para catálogos.
        /// </summary>
        /// 
        [RequireAccion("roles.ver")]
        [HttpGet("combo")]
        [AllowAnonymous] // o [Authorize] si quieres requerir token
        public IActionResult GetCombo()
        {
            try
            {
                var lista = _roles.GetLista()
                    .Where(x => x.estatus == true)
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
