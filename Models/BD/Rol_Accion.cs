using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_asemp.Models.BD
{
    [PrimaryKey(nameof(rol_id), nameof(accion_id))]
    public class Rol_Accion
    {
        [Column("rol_id")]
        public int rol_id { get; set; }

        [ForeignKey(nameof(rol_id))]
        public Rol? rol { get; set; }

        [Column("accion_id")]
        public int accion_id { get; set; }

        [ForeignKey(nameof(accion_id))]
        public Accion? accion { get; set; }
    }
}
