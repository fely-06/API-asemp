using API_asemp.Contextos;
using API_asemp.Models.BD;

namespace API_asemp.Datos
{
    public class VerificacionesSAT_Datos
    {
        private readonly myDbContext _bd;

        public VerificacionesSAT_Datos(myDbContext bd)
        {
            _bd = bd;
        }

        // Obtener historial por solicitud
        public List<VerificacionesSAT> ObtenerHistorial(int solicitudId)
        {
            return _bd.verificaciones_sat
                .Where(v => v.SolicitudId == solicitudId)
                .OrderByDescending(v => v.FechaVerificacion)
                .ToList();
        }
    }
}
