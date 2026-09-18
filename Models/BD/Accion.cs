using System.ComponentModel.DataAnnotations.Schema;

namespace API_asemp.Models.BD
{
    // Representa una acción o permiso dentro del sistema.
    // Ejemplo: "Registrar Cliente", "Editar Usuario", "Eliminar Rol", etc.
    public class Accion
    {
        public int id { get; set; }

        // Nombre de la acción (único dentro de su catálogo)
        public string? nombre { get; set; }

        // FK que indica a qué catálogo (módulo) pertenece la acción
        [Column("catalogo_id")]
        public int catalogo_id { get; set; }

        [ForeignKey(nameof(catalogo_id))]
        public Catalogo? catalogo { get; set; }
    }
}
