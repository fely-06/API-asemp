using API_asemp.Contextos;
using API_asemp.Models.BD;
using API_asemp.Models;
using Herramientas.Validaciones;

namespace API_asemp.Datos
{
    // Clase encargada de manejar las operaciones CRUD de la tabla Catalogos.
    public class Catalogos
    {
        private readonly myDbContext _db;

        // Inyección del contexto de base de datos.
        public Catalogos(myDbContext db)
        {
            _db = db;
        }

        // ===============================================================
        // ========== MÉTODOS DE CONSULTA Y MANEJO DE DATOS ==============
        // ===============================================================

        // ----------- Obtener un catálogo por ID -----------------------
        public Catalogo Get(int id)
        {
            var registro = _db.catalogos.FirstOrDefault(x => x.id == id);
            Ensure.ValidarNulo(registro, "No se encontró este catálogo");
            return registro!;
        }

        // ----------- Listar todos los catálogos -----------------------
        public List<Catalogo> GetLista()
        {
            return _db.catalogos
                .OrderBy(x => x.nombre)
                .ToList();
        }

        // ----------- Registrar nuevo catálogo -------------------------
        public Estatus<bool> Registrar(Catalogo catalogo)
        {
            // Validación de campos requeridos
            Ensure.ValidarVacio(catalogo.nombre!, "El nombre del catálogo no puede estar vacío");

            // Verificar duplicado
            if (_db.catalogos.Any(c => c.nombre!.ToLower() == catalogo.nombre!.ToLower()))
                return Estatus<bool>.Error("Ya existe un catálogo con este nombre.");

            _db.catalogos.Add(catalogo);
            _db.SaveChanges();
            return Estatus<bool>.OK("Catálogo registrado correctamente");
        }

        // ----------- Actualizar catálogo existente --------------------
        public Estatus<bool> Actualizar(Catalogo catalogo)
        {
            Ensure.ValidarNulo(catalogo, "El objeto catálogo no puede ser nulo");

            var registro = Get(catalogo.id);
            registro.nombre = catalogo.nombre;

            _db.SaveChanges();
            return Estatus<bool>.OK("Catálogo actualizado correctamente");
        }

        // ----------- Eliminar catálogo -------------------------------
        public Estatus<bool> Eliminar(int id)
        {
            var registro = Get(id);
            _db.catalogos.Remove(registro);
            _db.SaveChanges();
            return Estatus<bool>.OK("Catálogo eliminado correctamente");
        }
    }
}
