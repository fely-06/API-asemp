using API_asemp.Contextos;
using API_asemp.Models.BD;
using API_asemp.Models;
using Herramientas.Validaciones;

namespace API_asemp.Datos
{
    public class Roles
    {
        private readonly myDbContext _db;

        public Roles(myDbContext db)
        {
            _db = db;
        }

        public Rol Get(int id)
        {
            var registro = _db.roles.FirstOrDefault(x => x.id == id);
            Ensure.ValidarNulo(registro, "No se encontró este rol");
            return registro;
        }
        
       
        public List<Rol> GetLista()
        {
            // Podrías querer ordenar por nombre o id
            return _db.roles.OrderBy(x => x.nombre).ToList();
        }

        public Estatus<bool> Registrar(Rol rol)
        {
            Ensure.ValidarVacio(rol.nombre, "El nombre del rol no puede estar vacío");

            if (_db.roles.Any(r => r.nombre.ToLower() == rol.nombre.ToLower()))
            {
                return Estatus<bool>.Error("Ya existe un rol con este nombre.");
            }
            _db.roles.Add(rol);
            _db.SaveChanges();
            return Estatus<bool>.OK("Rol guardado");
        }

        public Estatus<bool> Actualizar(Rol rol)
        {
            Ensure.ValidarVacio(rol.nombre, "El nombre del rol no puede estar vacío");
            var registro = Get(rol.id);

            if (_db.roles.Any(r => r.id != rol.id && r.nombre.ToLower() == rol.nombre.ToLower()))
            {
                return Estatus<bool>.Error("Ya existe otro rol con este nombre.");
            }

            registro.nombre = rol.nombre;
            _db.SaveChanges();
            return Estatus<bool>.OK("Rol actualizado");
        }

        public Estatus<bool> Eliminar(int id)
        {
            var registro = Get(id);
            _db.roles.Remove(registro);
            _db.SaveChanges();
            return Estatus<bool>.OK("Rol eliminado");
        }
    }
}
