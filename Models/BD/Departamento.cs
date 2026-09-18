using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_asemp.Models.BD
{
    [Table("departamentos")]
    [PrimaryKey(nameof(id))]
    public class Departamento
    {
        [Column("id")]
        public int id { get; set; }

        [Column("nombre")]
        public string? nombre { get; set; }

        [Column("descripcion")]
        public string? descripcion { get; set; }

        // Relación con usuarios (1 departamento → N usuarios)
        public ICollection<Usuario>? usuarios { get; set; }
    }
}
