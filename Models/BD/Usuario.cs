using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_asemp.Models.BD
{
    public class Usuario
    {
        public int id { get; set; }
        public string? nombres { get; set; }
        public string? apellido_paterno { get; set; }
        public string? apellido_materno { get; set; }
        public string? correo { get; set; }
        public string? usuario { get; set; }
        public string? contrasena { get; set; }
        public bool estatus { get; set; }

        // Relaciones
        [Column("rol_id")]
        public int rol_id { get; set; }

        [ForeignKey(nameof(rol_id))]
        public Rol? rol { get; set; }

        [Column("departamento_id")]
        public int? departamento_id { get; set; }

        [ForeignKey(nameof(departamento_id))]
        public Departamento? departamento { get; set; }
    }
}
