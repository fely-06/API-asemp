using API_asemp.Contextos;
using API_asemp.Models.BD;
using API_asemp.Models;
using Herramientas.Validaciones;

namespace API_asemp.Datos
{
    public class CertificadosSAT
    {
        private readonly myDbContext _db;

        public CertificadosSAT(myDbContext db)
        {
            _db = db;
        }

        // Obtener certificado por ID
        public CertificadoSAT Get(int id)
        {
            var registro = _db.certificados_sat.FirstOrDefault(x => x.id == id);
            Ensure.ValidarNulo(registro, "No se encontró este certificado SAT");
            return registro!;
        }

        // Lista completa
        public List<CertificadoSAT> GetLista()
        {
            return _db.certificados_sat
                .OrderByDescending(x => x.id)
                .ToList();
        }

        // Registrar nuevo
        public Estatus<bool> Registrar(CertificadoSAT cert)
        {
            Ensure.ValidarEnteroPositivo(cert.cliente_id, "El cliente_id no es válido");
            Ensure.ValidarVacio(cert.archivo_pfx!, "Debe especificarse el archivo PFX");
            Ensure.ValidarVacio(cert.contrasena!, "Debe proporcionar la contraseña del certificado");

            // RFC obligatorio
            Ensure.ValidarVacio(cert.rfc!, "Debe especificarse el RFC del certificado");

            // Validar duplicado
            if (_db.certificados_sat.Any(c => c.cliente_id == cert.cliente_id))
                return Estatus<bool>.Error("Ya existe un certificado registrado para este cliente.");

            cert.fecha_registro = DateTime.UtcNow;
            cert.estatus = true;

            _db.certificados_sat.Add(cert);
            _db.SaveChanges();
            return Estatus<bool>.OK("Certificado SAT registrado correctamente");
        }

        // Actualizar existente
        public Estatus<bool> Actualizar(CertificadoSAT cert)
        {
            Ensure.ValidarNulo(cert, "El objeto certificado no puede ser nulo");

            var registro = Get(cert.id);

            registro.cliente_id = cert.cliente_id;
            registro.rfc = cert.rfc;
            registro.archivo_pfx = cert.archivo_pfx;
            registro.archivo_cer = cert.archivo_cer;
            registro.archivo_key = cert.archivo_key;
            registro.contrasena = cert.contrasena;
            registro.fecha_vigencia_inicio = cert.fecha_vigencia_inicio;
            registro.fecha_vigencia_fin = cert.fecha_vigencia_fin;
            registro.estatus = cert.estatus;

            _db.SaveChanges();
            return Estatus<bool>.OK("Certificado SAT actualizado correctamente");
        }

        // Eliminar
        public Estatus<bool> Eliminar(int id)
        {
            var registro = Get(id);
            _db.certificados_sat.Remove(registro);
            _db.SaveChanges();
            return Estatus<bool>.OK("Certificado SAT eliminado correctamente");
        }
    }
}
