using API_asemp.Contextos;
using API_asemp.Models.BD;
using API_asemp.Models;
using Herramientas.Validaciones;
using System.Linq;

namespace API_asemp.Datos
{
    public class Roles_Acciones
    {
        private readonly myDbContext _db;
        public Roles_Acciones(myDbContext db)
        {
            _db = db;
        }

        // Obtener relación por rol_id y accion_id
        public Rol_Accion Get(int rol_id, int accion_id)
        {
            var registro = _db.roles_acciones
                .FirstOrDefault(x => x.rol_id == rol_id && x.accion_id == accion_id);
            Ensure.ValidarNulo(registro, "No se encontró esta relación Rol-Acción");
            return registro!;
        }

        // Listar todas las relaciones
        public List<Rol_Accion> GetLista()
        {
            return _db.roles_acciones
                .OrderBy(x => x.rol_id)
                .ThenBy(x => x.accion_id)
                .ToList();
        }

        // Registrar nueva relación
        public Estatus<bool> Registrar(Rol_Accion relacion)
        {
            Ensure.ValidarEnteroPositivo(relacion.rol_id, "El rol_id no es válido");
            Ensure.ValidarEnteroPositivo(relacion.accion_id, "El accion_id no es válido");

            if (_db.roles_acciones.Any(r => r.rol_id == relacion.rol_id && r.accion_id == relacion.accion_id))
                return Estatus<bool>.Error("Esta relación ya existe.");

            _db.roles_acciones.Add(relacion);
            _db.SaveChanges();
            return Estatus<bool>.OK("Acción asignada correctamente al rol");
        }

        // Eliminar relación
        public Estatus<bool> Eliminar(int rol_id, int accion_id)
        {
            var registro = Get(rol_id, accion_id);
            _db.roles_acciones.Remove(registro);
            _db.SaveChanges();
            return Estatus<bool>.OK("Relación eliminada correctamente");
        }
    }
}
