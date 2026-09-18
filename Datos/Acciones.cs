using API_asemp.Contextos;
using API_asemp.Models.BD;
using API_asemp.Models;
using Herramientas.Validaciones;

namespace API_asemp.Datos
{
    // Clase que contiene las operaciones CRUD sobre la tabla Acciones
    public class Acciones
    {
        private readonly myDbContext _db;

        // Inyección del contexto de base de datos
        public Acciones(myDbContext db)
        {
            _db = db;
        }

        // ===============================================================
        // ========== MÉTODOS DE CONSULTA Y MANEJO DE DATOS ==============
        // ===============================================================

        // ----------- Obtener una acción por ID ------------------------
        public Accion Get(int id)
        {
            var registro = _db.acciones.FirstOrDefault(x => x.id == id);
            Ensure.ValidarNulo(registro, "No se encontró esta acción");
            return registro!;
        }

        // ----------- Listar todas las acciones ------------------------
        public List<Accion> GetLista()
        {
            return _db.acciones
                .OrderBy(x => x.nombre)
                .ToList();
        }

        // ----------- Registrar nueva acción ---------------------------
        public Estatus<bool> Registrar(Accion accion)
        {
            // Validaciones básicas
            Ensure.ValidarVacio(accion.nombre!, "El nombre de la acción no puede estar vacío");
            Ensure.ValidarEnteroPositivo(accion.catalogo_id, "Debe seleccionar un catálogo");

            // Verificar duplicado (mismo nombre dentro del mismo catálogo)
            if (_db.acciones.Any(a => a.nombre!.ToLower() == accion.nombre!.ToLower() &&
                                      a.catalogo_id == accion.catalogo_id))
                return Estatus<bool>.Error("Ya existe esta acción en el catálogo seleccionado.");

            _db.acciones.Add(accion);
            _db.SaveChanges();
            return Estatus<bool>.OK("Acción registrada correctamente");
        }

        // ----------- Actualizar acción existente ----------------------
        public Estatus<bool> Actualizar(Accion accion)
        {
            Ensure.ValidarNulo(accion, "El objeto acción no puede ser nulo");

            var registro = Get(accion.id);
            registro.nombre = accion.nombre;
            registro.catalogo_id = accion.catalogo_id;

            _db.SaveChanges();
            return Estatus<bool>.OK("Acción actualizada correctamente");
        }

        // ----------- Eliminar acción ---------------------------------
        public Estatus<bool> Eliminar(int id)
        {
            var registro = Get(id);
            _db.acciones.Remove(registro);
            _db.SaveChanges();
            return Estatus<bool>.OK("Acción eliminada correctamente");
        }





        //roles
        // 🔥 Nuevo método: devolver acciones agrupadas por catálogo
        public List<object> GetAgrupado()
        {
            var acciones = _db.acciones.ToList();
            var catalogos = _db.catalogos.ToList();

            var resultado = catalogos.Select(cat => new {
                catalogo = cat.nombre,
                acciones = acciones
                    .Where(a => a.catalogo_id == cat.id)
                    .Select(a => new {
                        id = a.id,
                        clave = a.nombre   // ← ya no includes descripcion
                    }).ToList()
            }).ToList<object>();

            return resultado;
        }

    }
}
