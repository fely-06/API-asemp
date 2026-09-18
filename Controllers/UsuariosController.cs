using API_asemp.Datos;
using API_asemp.Models.BD;
using Herramientas.Validaciones;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // ← necesario para [Authorize]

namespace API_asemp.Controllers
{
    // Controlador para manejar los usuarios del sistema.
    // Ruta base: /usuarios
    [RequireAccion("usuarios.ver")]
    [ApiController]
    [Route("usuarios")]
    [Authorize] // ← requiere token JWT válido para acceder
    public class UsuariosController : ControllerBase
    {
        private readonly Usuarios _usuarios;

        // Se recibe la clase Usuarios desde la capa de datos
        public UsuariosController(Usuarios usuariosService)
        {
            _usuarios = usuariosService;
        }

        // ===============================================================
        // ========== MÉTODOS DISPONIBLES EN LA API ======================
        // ===============================================================

        /// <summary>
        /// Devuelve todos los usuarios registrados.
        /// </summary>
        /// 
        [RequireAccion("usuarios.ver")]
        [HttpGet("lista")]
        public IActionResult GetLista()
        {
            try
            {
                var lista = _usuarios.GetLista();
                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Busca un usuario por su ID.
        /// </summary>
        /// 
        [RequireAccion("usuarios.ver")]
        [HttpGet("buscar/{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _usuarios.Get(id);
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Registra un nuevo usuario.
        /// </summary>
        /// 
        [RequireAccion("usuarios.crear")]
        [HttpPost("registrar")]
        public IActionResult Registrar([FromBody] Usuario usuario)
        {
            try
            {
                Ensure.ValidarNulo(usuario, "El objeto usuario no puede ser nulo.");

                var resultado = _usuarios.Registrar(usuario);
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
        /// Edita los datos de un usuario existente.
        /// </summary>
        /// 
        [RequireAccion("usuarios.editar")]
        [HttpPut("editar/{id}")]
        public IActionResult Editar(int id, [FromBody] Usuario usuario)
        {
            try
            {
                Ensure.ValidarNulo(usuario, "El objeto usuario no puede ser nulo.");
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                usuario.id = id;

                var resultado = _usuarios.Actualizar(usuario);
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
        /// Elimina un usuario por su ID.
        /// </summary>
        /// 
        [RequireAccion("usuarios.eliminar")]
        [HttpDelete("eliminar/{id}")]
        public IActionResult Eliminar(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _usuarios.Eliminar(id);
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
        /// Devuelve los usuarios con rol "cliente" que no están asignados a ningún cliente.
        /// </summary>
        /// 
        [RequireAccion("usuarios.ver")]
        [HttpGet("disponibles")]
        public IActionResult GetUsuariosDisponibles()
        {
            try
            {
                var lista = _usuarios.GetUsuariosDisponibles();
                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

    }
}
