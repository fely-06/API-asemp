using API_asemp.Contextos;
using API_asemp.Models.BD;
using API_asemp.Models;
using Herramientas.Validaciones;

namespace API_asemp.Datos
{
    public class DescargasSAT
    {
        private readonly myDbContext _db;

        public DescargasSAT(myDbContext db)
        {
            _db = db;
        }

        // =======================================
        //   LISTA POR SOLICITUD
        // =======================================
        public List<DescargaSAT> GetListaPorSolicitud(int solicitudId)
        {
            Ensure.ValidarEnteroPositivo(solicitudId, "El solicitud_id no es válido");
            return _db.descargas_sat
                .Where(x => x.solicitud_id == solicitudId)
                .OrderByDescending(x => x.fecha_descarga)
                .ToList();
        }

        // =======================================
        //   ÚLTIMA POR SOLICITUD
        // =======================================
        public DescargaSAT? GetUltimaPorSolicitud(int solicitudId)
        {
            Ensure.ValidarEnteroPositivo(solicitudId, "El solicitud_id no es válido");
            return _db.descargas_sat
                .Where(x => x.solicitud_id == solicitudId)
                .OrderByDescending(x => x.fecha_descarga)
                .FirstOrDefault();
        }

        // =======================================
        //   EXISTE ALGUNA POR SOLICITUD
        // =======================================
        public bool ExisteParaSolicitud(int solicitudId)
        {
            Ensure.ValidarEnteroPositivo(solicitudId, "El solicitud_id no es válido");
            return _db.descargas_sat.Any(x => x.solicitud_id == solicitudId);
        }



       //--------------------------------------------------------------------------\\

        // =======================================
        //   OBTENER DESCARGA POR ID
        // =======================================
        public DescargaSAT Get(int id)
        {
            var registro = _db.descargas_sat.FirstOrDefault(x => x.id == id);
            Ensure.ValidarNulo(registro, "No se encontró esta descarga SAT");
            return registro!;
        }

        // =======================================
        //   LISTA DE TODAS LAS DESCARGAS
        // =======================================
        public List<DescargaSAT> GetLista()
        {
            return _db.descargas_sat
                .OrderByDescending(x => x.fecha_descarga)
                .ToList();
        }

        // =======================================
        //   REGISTRAR DESCARGA
        // =======================================
        public Estatus<bool> Registrar(DescargaSAT descarga)
        {
            Ensure.ValidarEnteroPositivo(descarga.solicitud_id, "El solicitud_id no es válido");
            Ensure.ValidarVacio(descarga.archivo_zip!, "Debe especificar el nombre o ruta del archivo ZIP");

            descarga.fecha_descarga = DateTime.UtcNow;
            descarga.estatus ??= "Completada";

            _db.descargas_sat.Add(descarga);
            _db.SaveChanges();
            return Estatus<bool>.OK("Descarga SAT registrada correctamente");
        }

        // =======================================
        //   ACTUALIZAR DESCARGA
        // =======================================
        public Estatus<bool> Actualizar(DescargaSAT descarga)
        {
            Ensure.ValidarNulo(descarga, "El objeto descarga no puede ser nulo");
            var registro = Get(descarga.id);

            registro.solicitud_id = descarga.solicitud_id;
            registro.archivo_zip = descarga.archivo_zip;
            registro.carpeta_extraccion = descarga.carpeta_extraccion;
            registro.fecha_descarga = descarga.fecha_descarga;
            registro.estatus = descarga.estatus;

            _db.SaveChanges();
            return Estatus<bool>.OK("Descarga SAT actualizada correctamente");
        }

        // =======================================
        //   ELIMINAR DESCARGA
        // =======================================
        public Estatus<bool> Eliminar(int id)
        {
            var registro = Get(id);
            _db.descargas_sat.Remove(registro);
            _db.SaveChanges();
            return Estatus<bool>.OK("Descarga SAT eliminada correctamente");
        }
    }
}
