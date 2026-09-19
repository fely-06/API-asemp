using API_asemp.Contextos;
using API_asemp.Datos;
using API_asemp.Models.BD;
using API_asemp.Models.DTO; // 👈 agrega este using arriba
using API_asemp.Servicios;
using API_asemp.Utilerias;
using Herramientas.Validaciones;
using Microsoft.AspNetCore.Authorization; // ← necesario para [Authorize]
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace API_asemp.Controllers
{
    // Controlador para manejar los certificados SAT.
    // Ruta base: /certificadossat
    [RequireAccion("certificados.ver")]

    [ApiController]
    [Route("certificadossat")]
    [Authorize] // ← requiere token JWT válido para acceder
    public class CertificadosSATController : ControllerBase
    {
        private readonly CertificadosSAT _certificados;
        private readonly SatService _satService;
        private readonly myDbContext _db;      // 👈 aquí guardamos el DbContext
        private readonly IEmailService _emailService;

        // Se recibe la clase CertificadosSAT desde la capa de datos
        public CertificadosSATController(
            CertificadosSAT certificadosService,
            SatService satService, myDbContext db,
            IEmailService emailService)
        {
            _certificados = certificadosService;
            _satService = satService;
            _db = db;
            _emailService = emailService;
        }

        // BasePath igual que en DescargasSATController
        private string BasePath => _satService.GetStoragePath();

        // Mismo helper SafeJoin que usas en DescargasSATController
        private static string? SafeJoin(string root, string? subpath)
        {
            if (string.IsNullOrWhiteSpace(subpath)) return null;

            var rootFull = Path.GetFullPath(root);
            var candidate = Path.GetFullPath(
                Path.IsPathRooted(subpath)
                    ? subpath
                    : Path.Combine(root, subpath)
            );

            return candidate.StartsWith(rootFull, System.StringComparison.OrdinalIgnoreCase)
                ? candidate
                : null;
        }

        // ===============================================================
        // ========== MÉTODOS DISPONIBLES EN LA API ======================
        // ===============================================================
        /// <summary>
        /// Devuelve todos los certificados registrados.
        /// Admin: ve todos.
        /// Cliente: ve solo sus certificados.
        /// </summary>
        [RequireAccion("certificados.ver")]
        [HttpGet("lista")]
        public IActionResult GetLista()
        {
            try
            {
                // 1) Base: join certificado + cliente
                var query =
                    from c in _db.certificados_sat
                    join cli in _db.clientes on c.cliente_id equals cli.id
                    select new
                    {
                        c.id,
                        c.cliente_id,
                        cliente = cli.razon_social,
                        cli.usuario_id,        // ← clave para filtrar
                        c.rfc,
                        c.archivo_cer,
                        c.archivo_key,
                        c.archivo_pfx,


                        // 🔥 AQUÍ
                        c.fecha_vigencia_inicio,
                        c.fecha_vigencia_fin

                    };

                // 2) Obtener el ID del usuario desde el token JWT
                var userIdStr = User.FindFirst("id")?.Value
                                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdStr, out var userId))
                    return Unauthorized("Token inválido.");

                // 3) Obtener el rol del usuario
                var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

                // 4) Si no es admin, filtrar solo sus clientes
                // 4) SOLO el cliente se filtra
                if (rol.Equals("Cliente", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.usuario_id == userId);
                }


                // 5) Resultado
                var lista = query.ToList();
                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }




        /// <summary>
        /// Busca un certificado por su ID.
        /// </summary>
        [RequireAccion("certificados.ver")]
        [HttpGet("buscar/{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _certificados.Get(id);
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Agrega un nuevo certificado.
        /// </summary>
        [RequireAccion("certificados.subir")]
        [HttpPost("registrar")]
        public IActionResult Registrar([FromBody] CertificadoSAT cert)
        {
            try
            {
                Ensure.ValidarNulo(cert, "El objeto certificado no puede ser nulo.");

                var resultado = _certificados.Registrar(cert);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Edita un certificado existente.
        /// </summary>
        [RequireAccion("certificados.editar")]
        [HttpPut("editar/{id}")]
        public IActionResult Editar(int id, [FromBody] CertificadoSAT cert)
        {
            try
            {
                Ensure.ValidarNulo(cert, "El objeto certificado no puede ser nulo.");
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                cert.id = id;

                var resultado = _certificados.Actualizar(cert);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Elimina un certificado por su ID.
        /// </summary>
        [RequireAccion("certificados.eliminar")]
        [HttpDelete("eliminar/{id}")]
        public IActionResult Eliminar(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _certificados.Eliminar(id);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }





        //---------------------------------------------------\\
        /// <summary>
        /// Lee el archivo .cer y devuelve el RFC contenido en él.
        /// </summary>
        [RequireAccion("certificados.ver")]
        [HttpPost("leerRFC")]
        [Consumes("multipart/form-data")]
        [AllowAnonymous]
        public IActionResult LeerRFC([FromForm] CertificadoArchivosDTO data)
        {
            try
            {
                if (data.cer is null) return BadRequest("Debes subir el archivo .cer");

                // Leer bytes del .cer
                byte[] cerBytes;
                using (var ms = new MemoryStream())
                {
                    data.cer.CopyTo(ms);
                    cerBytes = ms.ToArray();
                }

                // Usar BouncyCastle para no perder atributos del DN
                var parser = new Org.BouncyCastle.X509.X509CertificateParser();
                var bc = parser.ReadCertificate(cerBytes);

                // 1) Barrer atributos del SubjectDN y buscar RFC en 2.5.4.45 o en 2.5.4.5 (serialNumber)
                string? rfc = null;
                var oids = bc.SubjectDN.GetOidList();
                for (int i = 0; i < oids.Count; i++)
                {
                    var oid = (Org.BouncyCastle.Asn1.DerObjectIdentifier)oids[i];
                    var vals = bc.SubjectDN.GetValueList(oid);
                    if (vals.Count == 0) continue;

                    var val = vals[0]?.ToString() ?? string.Empty;

                    // OID 2.5.4.45 puede venir como "RFC=XXXX;CURP=YYYY"
                    if (oid.Id == "2.5.4.45")
                    {
                        var m = System.Text.RegularExpressions.Regex.Match(
                            val, @"RFC\s*=\s*([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3})",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (m.Success) { rfc = m.Groups[1].Value.ToUpperInvariant(); break; }
                    }

                    // OID 2.5.4.5 (serialNumber) a veces trae directamente el RFC
                    if (oid.Id == "2.5.4.5" && string.IsNullOrEmpty(rfc))
                    {
                        var m = System.Text.RegularExpressions.Regex.Match(
                            val, @"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (m.Success) { rfc = m.Value.ToUpperInvariant(); break; }

                        // si viene "RFC=XXXX..." dentro del serialNumber
                        m = System.Text.RegularExpressions.Regex.Match(
                            val, @"RFC\s*=\s*([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3})",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (m.Success) { rfc = m.Groups[1].Value.ToUpperInvariant(); break; }
                    }
                }

                // 2) Fallback: escanear todo el DN crudo y evitar confundir CURP
                if (string.IsNullOrEmpty(rfc))
                {
                    var dn = bc.SubjectDN.ToString(); // crudo, con OIDs
                    var cand = System.Text.RegularExpressions.Regex.Matches(
                        dn, @"[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                        .Select(m => m.Value.ToUpperInvariant())
                        .ToList();

                    // filtro anti-CURP: posición 11 H/M y 12-13 estado
                    var estados = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "AS","BC","BS","CC","CL","CM","CS","CH","DF","DG","GT","GR","HG","JC","MC","MN",
              "MS","NT","NL","OC","PL","QT","QR","SP","SL","SR","TC","TL","TS","VZ","YN","ZS","NE" };

                    foreach (var c in cand)
                    {
                        if (!((c[10] == 'H' || c[10] == 'M') && estados.Contains(c.Substring(11, 2))))
                        { rfc = c; break; }
                    }
                }

                if (string.IsNullOrWhiteSpace(rfc))
                    return BadRequest("No se pudo extraer el RFC del certificado.");

                // También devuelve nombre y vigencias
                var certNet = new X509Certificate2(cerBytes);
                return Ok(new
                {
                    rfc,
                    nombre = certNet.GetNameInfo(X509NameType.SimpleName, false),
                    vigencia_inicio = certNet.NotBefore.ToString("yyyy-MM-dd"),
                    vigencia_fin = certNet.NotAfter.ToString("yyyy-MM-dd"),
                    valido = false,
                    mensaje = "RFC leído correctamente desde el certificado."
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al leer el certificado: {ex.Message}");
            }
        }


        //==============================================================================================================================\\
        // ===============================================================
        // DESCARGA DE ARCHIVOS (.cer, .key, .pfx, contraseña)
        // ===============================================================
        [RequireAccion("certificados.ver")]
        [HttpGet("descargar/{id:int}/{tipo}")]
        public IActionResult Descargar(int id, string tipo)
        {
            var cert = _certificados.Get(id);
            if (cert == null)
                return NotFound("Certificado no encontrado.");

            tipo = (tipo ?? "").ToLowerInvariant();

            // PASSWORD: se genera dinámico como .txt
            if (tipo == "password")
            {
                var contenido = cert.contrasena ?? string.Empty;
                var bytes = Encoding.UTF8.GetBytes(contenido);
                return File(bytes, "text/plain", $"password_{cert.id}.txt");
            }

            // Seleccionar el campo adecuado según el tipo
            string? rutaRel = tipo switch
            {
                "cer" => cert.archivo_cer,
                "key" => cert.archivo_key,
                "pfx" => cert.archivo_pfx,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(rutaRel))
                return NotFound("Ruta del archivo no registrada en el certificado.");

            // Resolver ruta física segura (igual patrón que DescargasSATController)
            var full = SafeJoin(BasePath, rutaRel) ?? rutaRel;

            if (!System.IO.File.Exists(full))
                return NotFound("Archivo no disponible en el servidor.");

            var extension = Path.GetExtension(full).ToLowerInvariant();

            string mime = extension switch
            {
                ".cer" => "application/x-x509-ca-cert",
                ".key" => "application/octet-stream",
                ".pfx" => "application/x-pkcs12",
                _ => "application/octet-stream"
            };

            // Mejor devolver stream para archivos potencialmente grandes
            var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, mime, Path.GetFileName(full));
        }


        //==============================================================================================================================\\
        // ===============================================================
        // ENVIAR POR CORREO (.cer, .key, .pfx, contraseña) EN UN ZIP
        // ===============================================================
        // POST certificadossat/enviar-correo/5
        // Body opcional: { "correo": "otro@correo.com" }
        // Si no se envía "correo", se usa el correo registrado del cliente.
        [RequireAccion("certificados.ver")]
        [HttpPost("enviar-correo/{id:int}")]
        public async Task<IActionResult> EnviarCorreo(int id, [FromBody] EnviarCertificadoCorreoDTO? body)
        {
            try
            {
                var cert = _certificados.Get(id);
                if (cert == null)
                    return NotFound("Certificado no encontrado.");

                // Resolver destinatario: el que venga en el body, o el correo del cliente dueño del certificado
                var destinatario = !string.IsNullOrWhiteSpace(body?.correo)
                    ? body!.correo!.Trim()
                    : _db.clientes.FirstOrDefault(c => c.id == cert.cliente_id)?.correo_electronico;

                if (string.IsNullOrWhiteSpace(destinatario))
                    return BadRequest("El cliente no tiene correo registrado. Especifica uno en el cuerpo de la petición (\"correo\").");

                // Archivos candidatos a incluir en el ZIP, con su nombre de salida
                var baseNombre = string.IsNullOrWhiteSpace(cert.rfc) ? $"cert_{cert.id}" : cert.rfc;
                var candidatos = new (string? rutaRel, string nombreEnZip)[]
                {
                    (cert.archivo_cer, $"{baseNombre}.cer"),
                    (cert.archivo_key, $"{baseNombre}.key"),
                    (cert.archivo_pfx, $"{baseNombre}.pfx"),
                };

                byte[] zipBytes;
                int archivosIncluidos = 0;

                using (var zipStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                    {
                        foreach (var (rutaRel, nombreEnZip) in candidatos)
                        {
                            if (string.IsNullOrWhiteSpace(rutaRel)) continue;

                            var full = SafeJoin(BasePath, rutaRel) ?? rutaRel;
                            if (!System.IO.File.Exists(full)) continue;

                            var entry = archive.CreateEntry(nombreEnZip, CompressionLevel.Optimal);
                            using var entryStream = entry.Open();
                            using var fileStream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
                            await fileStream.CopyToAsync(entryStream);
                            archivosIncluidos++;
                        }

                        // Incluir la contraseña como .txt dentro del mismo ZIP
                        if (!string.IsNullOrWhiteSpace(cert.contrasena))
                        {
                            var entry = archive.CreateEntry($"password_{baseNombre}.txt", CompressionLevel.Optimal);
                            using var entryStream = entry.Open();
                            var pwdBytes = Encoding.UTF8.GetBytes(cert.contrasena);
                            await entryStream.WriteAsync(pwdBytes);
                            archivosIncluidos++;
                        }
                    }

                    zipBytes = zipStream.ToArray();
                }

                if (archivosIncluidos == 0)
                    return NotFound("No hay archivos disponibles en el servidor para este certificado.");

                var nombreZip = $"certificado_{baseNombre}.zip";
                var asunto = $"Certificados SAT - {baseNombre}";
                var cuerpoHtml =
                    $"<p>Hola,</p>" +
                    $"<p>Adjunto encontrarás los archivos del certificado SAT con RFC <b>{baseNombre}</b> " +
                    $"comprimidos en un archivo ZIP.</p>" +
                    $"<p>Este correo fue generado automáticamente, por favor resguarda estos archivos con precaución.</p>";

                var adjunto = new EmailAttachment(nombreZip, zipBytes, "application/zip");
                await _emailService.EnviarConAdjuntosAsync(destinatario!, asunto, cuerpoHtml, new[] { adjunto });

                return Ok(new
                {
                    ok = true,
                    mensaje = $"Correo enviado a {destinatario}.",
                    destinatario,
                    archivo = nombreZip,
                    archivosIncluidos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, mensaje = $"Error enviando el correo: {ex.Message}" });
            }
        }




    }
}
