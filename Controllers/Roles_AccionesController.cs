using API_asemp.Datos;
using API_asemp.Models.BD;
using Herramientas.Validaciones;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // ← necesario para [Authorize]

namespace API_asemp.Controllers
{
    // Controlador para manejar las relaciones entre roles y acciones.
    // Ruta base: /rolesacciones
    [RequireAccion("roles.ver")]

    [ApiController]
    [Route("rolesacciones")]
    [Authorize] // ← requiere token JWT válido para acceder
    public class Roles_AccionesController : ControllerBase
    {
        private readonly Roles_Acciones _rolesAcciones;

        // Se recibe la clase Roles_Acciones desde la capa de datos
        public Roles_AccionesController(Roles_Acciones rolesAccionesService)
        {
            _rolesAcciones = rolesAccionesService;
        }

        // ===============================================================
        // ========== MÉTODOS DISPONIBLES EN LA API ======================
        // ===============================================================

        /// <summary>
        /// Devuelve todas las relaciones de roles con acciones.
        /// </summary>
        /// 
        [RequireAccion("roles.ver")]
        [HttpGet("lista")]
        public IActionResult GetLista()
        {
            try
            {
                var lista = _rolesAcciones.GetLista();
                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Busca una relación por rol_id y accion_id.
        /// </summary>
        /// 
        [RequireAccion("roles.ver")]
        [HttpGet("buscar/{rol_id}/{accion_id}")]
        public IActionResult GetByIds(int rol_id, int accion_id)
        {
            try
            {
                if (rol_id <= 0 || accion_id <= 0)
                    return BadRequest("Los IDs no son válidos.");

                var resultado = _rolesAcciones.Get(rol_id, accion_id);
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Crea una nueva relación entre rol y acción.
        /// </summary>
        /// 
        [RequireAccion("roles.permisos")]
        [HttpPost("registrar")]
        public IActionResult Registrar([FromBody] Rol_Accion rel)
        {
            try
            {
                Ensure.ValidarNulo(rel, "El objeto relación no puede ser nulo.");

                var resultado = _rolesAcciones.Registrar(rel);
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
        /// Elimina una relación por rol_id y accion_id.
        /// </summary>
        /// 
        [RequireAccion("roles.permisos")]
        [HttpDelete("eliminar/{rol_id}/{accion_id}")]
        public IActionResult Eliminar(int rol_id, int accion_id)
        {
            try
            {
                if (rol_id <= 0 || accion_id <= 0)
                    return BadRequest("Los IDs no son válidos.");

                var resultado = _rolesAcciones.Eliminar(rol_id, accion_id);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        //roles
        [RequireAccion("roles.ver")]
        [HttpGet("por-rol/{rolId}")]
        public IActionResult GetPorRol(int rolId)
        {
            var lista = _rolesAcciones.GetLista()
                .Where(x => x.rol_id == rolId)
                .Select(x => new {
                    idAccion = x.accion_id
                }).ToList();

            return Ok(lista);
        }


        public class PermisosDTO
        {
            public int idRol { get; set; }
            public List<int> acciones { get; set; } = new();
        }

        [HttpPost("guardar")]
        [RequireAccion("roles.permisos")]
        public IActionResult GuardarPermisos([FromBody] PermisosDTO dto)
        {
            // Borrar permisos actuales
            var actuales = _rolesAcciones.GetLista()
                .Where(x => x.rol_id == dto.idRol)
                .ToList();

            foreach (var r in actuales)
                _rolesAcciones.Eliminar(r.rol_id, r.accion_id);

            // Insertar nuevos
            foreach (var idAccion in dto.acciones)
            {
                _rolesAcciones.Registrar(new Rol_Accion
                {
                    rol_id = dto.idRol,
                    accion_id = idAccion
                });
            }

            return Ok(new { ok = true, mensaje = "Permisos actualizados" });
        }


    }
}
