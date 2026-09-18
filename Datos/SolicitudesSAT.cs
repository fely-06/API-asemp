using API_asemp.Contextos;
using API_asemp.Models.BD;
using API_asemp.Models;
using Herramientas.Validaciones;

namespace API_asemp.Datos
{
    public class SolicitudesSAT
    {
        private readonly myDbContext _db;

        // ✅ Exponer el DbContext para usarlo desde controladores
        public myDbContext DbContext => _db;

        public SolicitudesSAT(myDbContext db)
        {
            _db = db;
        }

        // ==============================================
        //   OBTENER UNA SOLICITUD POR ID
        // ==============================================
        public SolicitudSAT Get(int id)
        {
            var registro = _db.solicitudes_sat.FirstOrDefault(x => x.id == id);
            Ensure.ValidarNulo(registro, "No se encontró esta solicitud SAT");
            return registro!;
        }


        // ==============================================
        //   BÚSQUEDA PAGINADA + FILTROS (BACKEND REAL)
        // ==============================================
        public ResultadoPaginado<SolicitudSATListDTO> Buscar(SolicitudSATFiltro f)
        {
            // Query base con joins
            //var query = from s in _db.solicitudes_sat
            //            join u in _db.usuarios on s.usuario_id equals u.id
            //            join c in _db.clientes on s.cliente_id equals c.id
            //            select new { s, u, c };
            var query =
    from s in _db.solicitudes_sat

        // LEFT JOIN usuarios
    join u0 in _db.usuarios on s.usuario_id equals u0.id into ju
    from u in ju.DefaultIfEmpty()

        // LEFT JOIN clientes
    join c0 in _db.clientes on s.cliente_id equals c0.id into jc
    from c in jc.DefaultIfEmpty()

    select new { s, u, c };


            // Normalizar paginación
            if (f.pagina <= 0) f.pagina = 1;
            if (f.tamano <= 0) f.tamano = 25;

            // === FILTRO POR TEXTO GENERAL ===
            if (!string.IsNullOrWhiteSpace(f.texto))
            {
                var t = f.texto.Trim().ToLower();

                query = query.Where(x =>
                    (x.u.nombres ?? "").ToLower().Contains(t) ||
                    (x.u.apellido_paterno ?? "").ToLower().Contains(t) ||
                    (x.c.razon_social ?? "").ToLower().Contains(t) ||
                    (x.s.tipo_solicitud ?? "").ToLower().Contains(t) ||
                    (x.s.rfc_emisor ?? "").ToLower().Contains(t) ||
                    (x.s.rfc_receptor ?? "").ToLower().Contains(t)
                );
            }

            // === FILTRO POR ESTADO ===
            //if (f.estado.HasValue)
            //{
            //    query = query.Where(x => x.s.estado_solicitud == f.estado.Value);
            //}
            if (f.estadoLista != null && f.estadoLista.Any())
            {
                query = query.Where(x => f.estadoLista.Contains(x.s.estado_solicitud));
            }


            // === FILTRO POR TIPO ===
            if (!string.IsNullOrWhiteSpace(f.tipo))
            {
                var tipo = f.tipo.Trim().ToLower();
                query = query.Where(x => (x.s.tipo_solicitud ?? "").ToLower() == tipo);
            }

            // === FILTRO POR CLIENTE ===
            if (f.cliente_id.HasValue)
            {
                query = query.Where(x => x.s.cliente_id == f.cliente_id.Value);
            }

            // === FILTRO POR USUARIO ===
            if (f.usuario_id.HasValue)
            {
                query = query.Where(x => x.s.usuario_id == f.usuario_id.Value);
            }

            // === FILTRO POR FECHAS ===
            // === FILTRO POR FECHAS (STRING → DateTime?) ===
            DateTime? fi = TryParseFecha(f.fecha_inicio);
            DateTime? ff = TryParseFecha(f.fecha_fin);

            // Desde
            if (fi.HasValue)
            {
                var desde = fi.Value.Date.ToUniversalTime();
                query = query.Where(x => x.s.fecha_inicio >= desde);
            }

            // Hasta
            if (ff.HasValue)
            {
                var hasta = ff.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                query = query.Where(x => x.s.fecha_fin <= hasta);
            }



            // === TOTAL ANTES DEL PAGINADO ===
            var total = query.Count();

            // === PAGINADO ===
            var skip = (f.pagina - 1) * f.tamano;

            var pageQuery = query
                .OrderByDescending(x => x.s.id)
                .Skip(skip)
                .Take(f.tamano)
                .ToList();   // ejecución aquí

            // === MAPEO A DTO ===
            var datos = pageQuery.Select(x => new SolicitudSATListDTO
            {
                id = x.s.id,
                usuario_id = x.s.usuario_id,
                cliente_id = x.s.cliente_id,

                usuario = x.u.nombres + " " + x.u.apellido_paterno,
                cliente = x.c.razon_social,

                certificado_id = x.s.certificado_id,

                tipo_solicitud = x.s.tipo_solicitud,
                rfc_emisor = x.s.rfc_emisor,
                rfc_receptor = x.s.rfc_receptor,

                fecha_inicio = x.s.fecha_inicio,
                fecha_fin = x.s.fecha_fin,
                fecha_creacion = x.s.fecha_creacion,
                fecha_ultima_verificacion = x.s.fecha_ultima_verificacion,

                token = x.s.token,
                codigo_estado = x.s.codigo_estado,
                estado_solicitud = x.s.estado_solicitud,
                mensaje = x.s.mensaje,

                estado_solicitud_texto = SatTranslator.TraducirEstadoSolicitud(x.s.estado_solicitud),
                codigo_estado_texto = SatTranslator.TraducirCodigoEstado(x.s.codigo_estado)
            })
            .ToList();

            // === ARMAR RESPUESTA PAGINADA ===
            var paginas = (int)Math.Ceiling(total / (double)f.tamano);

            return new ResultadoPaginado<SolicitudSATListDTO>
            {
                total = total,
                pagina = f.pagina,
                tamano = f.tamano,
                paginas = paginas,
                datos = datos
            };
        }
        //=================================================================
        private static DateTime? TryParseFecha(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            // formato ISO (yyyy-MM-dd)
            if (DateTime.TryParseExact(valor, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var dt))
                return dt;

            // Parse común
            if (DateTime.TryParse(valor, out dt))
                return dt;

            return null; // no truena, simplemente no filtra
        }


        //================================================



        // ==============================================
        //   LISTA COMPLETA DE SOLICITUDES
        // ==============================================
        public List<SolicitudSATListDTO> GetLista()
        {
            var lista = (from s in _db.solicitudes_sat
                         join u in _db.usuarios on s.usuario_id equals u.id
                         join c in _db.clientes on s.cliente_id equals c.id
                         orderby s.id descending
                         select new SolicitudSATListDTO
                         {
                             // =============================
                             // Identificadores
                             // =============================
                             id = s.id,
                             usuario_id = s.usuario_id,
                             cliente_id = s.cliente_id,

                             // =============================
                             // Usuario / Cliente
                             // =============================
                             usuario = u.nombres + " " + u.apellido_paterno/*+ " " + u.apellido_materno*/,
                             cliente = c.razon_social,

                             // =============================
                             // Certificado
                             // =============================
                             certificado_id = s.certificado_id,

                             // =============================
                             // Datos de la solicitud
                             // =============================
                             tipo_solicitud = s.tipo_solicitud,
                             rfc_emisor = s.rfc_emisor,
                             rfc_receptor = s.rfc_receptor,

                             // =============================
                             // Fechas
                             // =============================
                             fecha_inicio = s.fecha_inicio,
                             fecha_fin = s.fecha_fin,
                             fecha_creacion = s.fecha_creacion,
                             fecha_ultima_verificacion = s.fecha_ultima_verificacion,

                             // =============================
                             // Datos SAT crudos
                             // =============================
                             token = s.token,
                             codigo_estado = s.codigo_estado,
                             estado_solicitud = s.estado_solicitud,
                             mensaje = s.mensaje,

                             // =============================
                             // Traducciones importantes (NUEVO)
                             // =============================
                             estado_solicitud_texto = SatTranslator.TraducirEstadoSolicitud(s.estado_solicitud),
                             codigo_estado_texto = SatTranslator.TraducirCodigoEstado(s.codigo_estado)
                         })
                         .ToList();

            return lista;
        }
        public static class SatTranslator
        {
            public static string TraducirEstadoSolicitud(int estado)
            {
                return estado switch
                {
                    1 => "Aceptada",
                    2 => "En proceso",
                    3 => "Terminada",
                    4 => "Error",
                    5 => "Rechazada",
                    6 => "Vencida",
                    _ => "Desconocido"
                };
            }

            public static string TraducirCodigoEstado(string? codigo)
            {
                return codigo switch
                {
                    "5000" => "Solicitud recibida con éxito",
                    "5002" => "Se agotaron las solicitudes",
                    "5003" => "Tope máximo excedido",
                    "5004" => "No se encontró información",
                    "5005" => "Solicitud duplicada",
                    "404" => "Error no controlado",
                    _ => "Desconocido"
                };
            }
        }




        // ==============================================
        //   REGISTRAR NUEVA SOLICITUD
        // ==============================================
        public Estatus<int> Registrar(SolicitudSAT solicitud)
        {
            Ensure.ValidarEnteroPositivo(solicitud.usuario_id, "usuario_id inválido");
            Ensure.ValidarEnteroPositivo(solicitud.cliente_id, "cliente_id inválido");
            Ensure.ValidarVacio(solicitud.tipo_solicitud!, "tipo_solicitud requerido");
            Ensure.ValidarFecha(solicitud.fecha_inicio, "fecha_inicio requerida");
            Ensure.ValidarFecha(solicitud.fecha_fin, "fecha_fin requerida");

            // 1) Resolver certificado activo del cliente
            var cert = _db.certificados_sat
                .FirstOrDefault(c => c.cliente_id == solicitud.cliente_id && c.estatus);
            Ensure.ValidarNulo(cert, "El cliente no tiene certificado activo");

            solicitud.certificado_id = cert!.id;
            solicitud.rfc_emisor = cert.rfc;                // ← tomar RFC de la tabla de certificados
                                                            // rfc_receptor queda null para 'Emitidos' (lo llenarás si el tipo requiere receptor)

            // 2) Estado inicial y campos SAT vacíos
            solicitud.estado_solicitud = 0;                    // CREADA
            solicitud.codigo_estado = null;
            solicitud.mensaje = null;
            solicitud.token = null;

            // 3) Fechas en UTC
            solicitud.fecha_inicio = solicitud.fecha_inicio.ToUniversalTime();
            solicitud.fecha_fin = solicitud.fecha_fin.ToUniversalTime();
            solicitud.fecha_creacion = DateTime.UtcNow;
            solicitud.fecha_ultima_verificacion = null;

            _db.solicitudes_sat.Add(solicitud);
            _db.SaveChanges();

            return Estatus<int>.OK(solicitud.id, "Solicitud creada");
        }


        // ==============================================
        //   ACTUALIZAR SOLICITUD
        // ==============================================
        public Estatus<bool> Actualizar(SolicitudSAT solicitud)
        {
            Ensure.ValidarNulo(solicitud, "El objeto solicitud no puede ser nulo");
            var registro = Get(solicitud.id);

            registro.usuario_id = solicitud.usuario_id;
            registro.cliente_id = solicitud.cliente_id;
            registro.certificado_id = solicitud.certificado_id;
            registro.tipo_solicitud = solicitud.tipo_solicitud;
            registro.rfc_emisor = solicitud.rfc_emisor;
            registro.rfc_receptor = solicitud.rfc_receptor;
            registro.fecha_inicio = solicitud.fecha_inicio;
            registro.fecha_fin = solicitud.fecha_fin;
            registro.token = solicitud.token;
            registro.codigo_estado = solicitud.codigo_estado;
            registro.estado_solicitud = solicitud.estado_solicitud;
            registro.mensaje = solicitud.mensaje;
            registro.fecha_ultima_verificacion = solicitud.fecha_ultima_verificacion;

            _db.SaveChanges();
            return Estatus<bool>.OK("Solicitud SAT actualizada correctamente");
        }

        // ==============================================
        //   ELIMINAR SOLICITUD
        // ==============================================
        public Estatus<bool> Eliminar(int id)
        {
            var registro = Get(id);
            _db.solicitudes_sat.Remove(registro);
            _db.SaveChanges();
            return Estatus<bool>.OK("Solicitud SAT eliminada correctamente");
        }
    }
}