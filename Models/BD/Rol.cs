using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_asemp.Models.BD
{
    [Table("roles")]
    [PrimaryKey(nameof(id))]
    public class Rol
    {
        [Column("id")]
        public int id { get; set; }

        [Column("nombre")]
        public string? nombre { get; set; }

        [Column("descripcion")]
        public string? descripcion { get; set; }

        [Column("estatus")]
        public bool estatus { get; set; }

        // Relación con usuarios (1 rol → N usuarios)
        public ICollection<Usuario>? usuarios { get; set; }

        // Relación con la tabla puente Roles_Acciones
        public ICollection<Rol_Accion>? roles_acciones { get; set; }
    }
}
