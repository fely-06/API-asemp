using System.ComponentModel.DataAnnotations.Schema;

namespace API_asemp.Models.BD
{
    // Representa un grupo o módulo del sistema al que pertenecen las acciones.
    // Ejemplo: "Usuarios", "Clientes", "SAT", "Configuración", etc.
    public class Catalogo
    {
        public int id { get; set; }

        // Nombre del catálogo o módulo
        public string? nombre { get; set; }

        // Relación uno a muchos (un catálogo puede tener muchas acciones)
        [InverseProperty(nameof(Accion.catalogo))]
        public ICollection<Accion>? acciones { get; set; }
    }
}
