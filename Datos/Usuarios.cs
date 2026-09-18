using API_asemp.Contextos;
using API_asemp.Models.BD;
using API_asemp.Models;
using Herramientas.Validaciones;
using BCrypt.Net; // 👈 importante para usar HashPassword y Verify

namespace API_asemp.Datos
{
    public class Usuarios
    {
        private readonly myDbContext _db;

        public Usuarios(myDbContext db)
        {
            _db = db;
        }

        // ================================
        //   OBTENER UN USUARIO POR ID
        // ================================
        public Usuario Get(int id)
        {
            var registro = _db.usuarios.FirstOrDefault(x => x.id == id);
            Ensure.ValidarNulo(registro, "No se encontró este usuario");
            return registro!;
        }

        // ================================
        //   LISTA DE TODOS LOS USUARIOS
        // ================================
        public List<object> GetLista()
        {
            return _db.usuarios
                .OrderByDescending(x => x.id)
                .Select(u => new
                {
                    u.id,
                    u.nombres,
                    u.apellido_paterno,
                    u.apellido_materno,
                    u.correo,
                    u.usuario,
                    u.estatus,
                    rol = _db.roles
                        .Where(r => r.id == u.rol_id)
                        .Select(r => new { r.id, r.nombre })
                        .FirstOrDefault(),
                    departamento = _db.departamentos
                        .Where(d => d.id == u.departamento_id)
                        .Select(d => new { d.id, d.nombre })
                        .FirstOrDefault()
                })
                .ToList<object>();
        }

        // ================================
        //   REGISTRAR NUEVO USUARIO
        // ================================
        public Estatus<bool> Registrar(Usuario usuario)
        {
            Ensure.ValidarVacio(usuario.usuario, "El usuario no puede estar vacío");
            Ensure.ValidarVacio(usuario.contrasena, "La contraseña no puede estar vacía");
            Ensure.ValidarVacio(usuario.correo, "El correo no puede estar vacío");

            if (_db.usuarios.Any(u => u.usuario!.ToLower() == usuario.usuario!.ToLower()))
                return Estatus<bool>.Error("Ya existe un usuario con este nombre.");

            // ✅ Validar departamento solo si el rol NO es cliente
            var rol = _db.roles.FirstOrDefault(r => r.id == usuario.rol_id);
            if (rol != null && rol.nombre.ToLower() != "cliente" && usuario.departamento_id == null)
                return Estatus<bool>.Error("Selecciona un departamento antes de continuar.");

            // ✅ Encriptar contraseña antes de guardar
            usuario.contrasena = BCrypt.Net.BCrypt.HashPassword(usuario.contrasena);

            usuario.estatus = true;

            _db.usuarios.Add(usuario);
            _db.SaveChanges();

            return Estatus<bool>.OK("Usuario guardado correctamente");
        }


        // ================================
        //   ACTUALIZAR USUARIO EXISTENTE
        // ================================
        public Estatus<bool> Actualizar(Usuario usuario)
        {
            Ensure.ValidarNulo(usuario, "El objeto usuario no puede ser nulo");

            var registro = Get(usuario.id);

            if (_db.usuarios.Any(u => u.id != usuario.id && u.usuario!.ToLower() == usuario.usuario!.ToLower()))
                return Estatus<bool>.Error("Ya existe otro usuario con este nombre.");

            // Actualizar campos normales
            registro.nombres = usuario.nombres;
            registro.apellido_paterno = usuario.apellido_paterno;
            registro.apellido_materno = usuario.apellido_materno;
            registro.correo = usuario.correo;
            registro.usuario = usuario.usuario;
            registro.departamento_id = usuario.departamento_id;
            registro.rol_id = usuario.rol_id;
            registro.estatus = usuario.estatus;

            // ✅ Si el usuario mandó una nueva contraseña, la encriptamos
            if (!string.IsNullOrWhiteSpace(usuario.contrasena))
            {
                registro.contrasena = BCrypt.Net.BCrypt.HashPassword(usuario.contrasena);
            }

            _db.SaveChanges();
            return Estatus<bool>.OK("Usuario actualizado correctamente");
        }

        // ================================
        //   ELIMINAR USUARIO POR ID
        // ================================
        public Estatus<bool> Eliminar(int id)
        {
            var registro = Get(id);
            _db.usuarios.Remove(registro);
            _db.SaveChanges();
            return Estatus<bool>.OK("Usuario eliminado correctamente");
        }



        // ================================
        //   USUARIOS DISPONIBLES PARA CLIENTES
        // ================================
        public List<object> GetUsuariosDisponibles()
        {
            var disponibles = _db.usuarios
                .Where(u =>
                    _db.roles
                        .Where(r => r.id == u.rol_id && r.nombre.ToLower() == "cliente")
                        .Any() &&
                    !_db.clientes.Any(c => c.usuario_id == u.id))
                .Select(u => new
                {
                    id = u.id,
                    usuario = u.usuario,
                    correo = u.correo,

                    // 🔥 AGREGAMOS NOMBRES Y APELLIDOS
                    nombres = u.nombres,
                    apellido_paterno = u.apellido_paterno,
                    apellido_materno = u.apellido_materno,

                    rol = _db.roles
                        .Where(r => r.id == u.rol_id)
                        .Select(r => r.nombre)
                        .FirstOrDefault()
                })
                .ToList<object>();

            return disponibles;
        }
    

    }
}
