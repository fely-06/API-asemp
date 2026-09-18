using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using API_asemp.Contextos;
using API_asemp.Datos;
using API_asemp.Models.BD;
using API_asemp.Servicios;
using ASEMP.CFDI.DescargaMasiva.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_asemp.Controllers
{
    [RequireAccion("sat.ver")]

    [ApiController]
    [Route("api/sat/descargas")]
    [Authorize]
    public class DescargasSATController : ControllerBase
    {
        private readonly myDbContext _db;
        private readonly DescargasSAT _repo;
        private readonly SatService _satService;

        public DescargasSATController(myDbContext db, SatService satService)
        {
            _db = db;
            _repo = new DescargasSAT(db);
            _satService = satService;
        }

        // ================== Helpers ==================
        private string BasePath => _satService.GetStoragePath();

        private static string? SafeJoin(string root, string? subpath)
        {
            if (string.IsNullOrWhiteSpace(subpath)) return null;
            var rootFull = Path.GetFullPath(root);
            var candidate = Path.GetFullPath(Path.IsPathRooted(subpath) ? subpath : Path.Combine(root, subpath));
            return candidate.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase) ? candidate : null;
        }

        private DescargaSAT? GetUltimaDescarga(int solicitudId)
        {
            return _db.descargas_sat
                .Where(x => x.solicitud_id == solicitudId)
                .OrderByDescending(x => x.fecha_descarga)
                .FirstOrDefault();
        }

        private static string PdfDirFromExtraccion(string carpetaExtraccion)
        {
            var parent = Path.GetDirectoryName(carpetaExtraccion) ?? carpetaExtraccion;
            return Path.Combine(parent, "PDF");
        }

        // ===============================================================
        // 🔹 DESCARGAR PAQUETE ZIP DESDE UNA SOLICITUD FINALIZADA (SAT)
        // ===============================================================
        [HttpPost("{solicitudId:int}/descargar")]
        public async Task<IActionResult> DescargarPaqueteDesdeSolicitud(int solicitudId)
        {
            try
            {
                var solicitud = _db.solicitudes_sat.FirstOrDefault(s => s.id == solicitudId);
                if (solicitud == null)
                    return NotFound("No se encontró la solicitud SAT.");

                var verificacion = _db.verificaciones_sat
                    .Where(v => v.SolicitudId == solicitudId)
                    .OrderByDescending(v => v.FechaVerificacion)
                    .FirstOrDefault();

                if (verificacion == null)
                    return BadRequest("No hay verificaciones registradas para esta solicitud.");
                if (verificacion.EstadoSolicitud != 3 || verificacion.CodigoEstado != "5000")
                    return BadRequest("La solicitud aún no está lista (Estado ≠ 3 o Código ≠ 5000).");

                var certificado = _db.certificados_sat.FirstOrDefault(c => c.id == solicitud.certificado_id);
                if (certificado == null)
                    return NotFound("No se encontró el certificado asociado a la solicitud.");

                string basePath = _satService.GetStoragePath();
                string rutaPfx = Path.Combine(basePath, certificado.archivo_pfx!);
                byte[] pfxBytes = await System.IO.File.ReadAllBytesAsync(rutaPfx);
                var cert = X509Certificate2Helper.GetCertificate(pfxBytes, certificado.contrasena!);

                var authRes = await _satService.AutenticacionAsync(cert);
                if (!authRes.Ok)
                    return BadRequest("Error autenticando con el SAT.");

                var accessToken = new ASEMP.CFDI.DescargaMasiva.Models.AccessToken(authRes.Token);

                string paqueteId = solicitud.token!;
                var resultadoSAT = await _satService.DescargarPaqueteAsync(paqueteId, certificado, accessToken, solicitud.tipo_solicitud
);

                if (resultadoSAT.Estado == "Error")
                    return BadRequest(new { ok = false, mensaje = resultadoSAT.Mensaje });

                var descarga = new DescargaSAT
                {
                    solicitud_id = solicitud.id,
                    archivo_zip = resultadoSAT.ArchivoZip,
                    carpeta_extraccion = resultadoSAT.CarpetaXml,
                    fecha_descarga = DateTime.UtcNow,
                    estatus = resultadoSAT.Estado
                };

                _repo.Registrar(descarga);

                return Ok(new
                {
                    ok = true,
                    mensaje = resultadoSAT.Mensaje,
                    estado = resultadoSAT.Estado,
                    archivoZip = resultadoSAT.ArchivoZip,
                    carpetaXml = resultadoSAT.CarpetaXml,
                    carpetaPdf = resultadoSAT.CarpetaPdf
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, mensaje = ex.Message });
            }
        }

        // ===============================================================
        // 🔹 LISTAR TODAS LAS DESCARGAS
        // ===============================================================
        [HttpGet("lista")]
        public IActionResult GetLista()
        {
            try
            {
                var lista = _repo.GetLista();
                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // ===============================================================
        // 🔹 HISTORIAL Y ÚLTIMA DESCARGA POR SOLICITUD
        // ===============================================================

        // GET api/sat/descargas/{solicitudId}/historial
        [HttpGet("{solicitudId:int}/historial")]
        public IActionResult Historial(int solicitudId)
        {
            var lista = _db.descargas_sat
                .Where(x => x.solicitud_id == solicitudId)
                .OrderByDescending(x => x.fecha_descarga)
                .ToList();
            return Ok(lista);
        }

        // GET api/sat/descargas/{solicitudId}/ultima
        [HttpGet("{solicitudId:int}/ultima")]
        public IActionResult Ultima(int solicitudId)
        {
            var d = GetUltimaDescarga(solicitudId);
            if (d == null) return NotFound("Sin descargas registradas para esta solicitud.");

            var carpetaPdf = d.carpeta_extraccion != null ? PdfDirFromExtraccion(d.carpeta_extraccion) : null;
            return Ok(new
            {
                d.id,
                d.solicitud_id,
                d.archivo_zip,
                d.carpeta_extraccion,
                d.fecha_descarga,
                d.estatus,
                carpeta_pdf = carpetaPdf
            });
        }

        // ===============================================================
        // 🔹 SERVIR ZIP / XML / PDF
        // ===============================================================

        // GET api/sat/descargas/{solicitudId}/zip
        [HttpGet("{solicitudId:int}/zip")]
        public IActionResult DescargarZip(int solicitudId)
        {
            var d = GetUltimaDescarga(solicitudId);
            if (d == null) return NotFound("Sin descargas para esta solicitud.");

            var candidatos = (d.archivo_zip ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var cand in candidatos)
            {
                var full = SafeJoin(BasePath, cand) ?? cand;
                if (System.IO.File.Exists(full))
                    return PhysicalFile(full, "application/zip", Path.GetFileName(full));
            }

            return NotFound("No se encontró ningún ZIP registrado.");
        }

        // GET api/sat/descargas/{solicitudId}/xml
        [HttpGet("{solicitudId:int}/xml")]
        public IActionResult DescargarXmlZip(int solicitudId)
        {
            var d = GetUltimaDescarga(solicitudId);
            if (d == null) return NotFound("Sin descargas para esta solicitud.");
            if (string.IsNullOrWhiteSpace(d.carpeta_extraccion)) return NotFound("No hay carpeta de extracción registrada.");

            var carpeta = SafeJoin(BasePath, d.carpeta_extraccion) ?? d.carpeta_extraccion;
            if (!Directory.Exists(carpeta)) return NotFound("La carpeta de XML no existe.");

            var tmpName = $"xml_{solicitudId}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
            var tmpFull = Path.Combine(Path.GetTempPath(), tmpName);
            if (System.IO.File.Exists(tmpFull)) System.IO.File.Delete(tmpFull);
            ZipFile.CreateFromDirectory(carpeta, tmpFull, CompressionLevel.SmallestSize, includeBaseDirectory: false);

            return PhysicalFile(tmpFull, "application/zip", tmpName);
        }

        // GET api/sat/descargas/{solicitudId}/pdf
        [HttpGet("{solicitudId:int}/pdf")]
        public IActionResult AbrirUltimoPdf(int solicitudId)
        {
            var d = GetUltimaDescarga(solicitudId);
            if (d == null) return NotFound("Sin descargas para esta solicitud.");
            if (string.IsNullOrWhiteSpace(d.carpeta_extraccion)) return NotFound("No hay carpeta de extracción registrada.");

            var pdfDir = PdfDirFromExtraccion(d.carpeta_extraccion);
            var fullPdfDir = SafeJoin(BasePath, pdfDir) ?? pdfDir;
            if (!Directory.Exists(fullPdfDir)) return NotFound("Carpeta PDF no existe.");

            var ultimo = Directory.GetFiles(fullPdfDir, "*.pdf")
                .Select(p => new FileInfo(p))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            if (ultimo == null) return NotFound("No hay PDFs disponibles.");
            return PhysicalFile(ultimo.FullName, "application/pdf", enableRangeProcessing: true);
        }

        // ===============================================================
        // 🔹 LISTAR Y ABRIR ARCHIVOS POR TIPO (pdf|xml)
        // ===============================================================

        // GET api/sat/descargas/{solicitudId}/archivos?tipo=pdf|xml
        [HttpGet("{solicitudId:int}/archivos")]
        public IActionResult ListarArchivos(int solicitudId, [FromQuery] string tipo = "pdf")
        {
            var d = GetUltimaDescarga(solicitudId);
            if (d == null) return NotFound("Sin descargas.");

            string? dirRel = null;
            if (tipo.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                dirRel = PdfDirFromExtraccion(d.carpeta_extraccion ?? "");
            else if (tipo.Equals("xml", StringComparison.OrdinalIgnoreCase))
                dirRel = d.carpeta_extraccion;
            else
                return BadRequest("tipo debe ser 'pdf' o 'xml'.");

            var dir = SafeJoin(BasePath, dirRel) ?? dirRel!;
            if (!Directory.Exists(dir)) return Ok(Array.Empty<object>());

            var patron = tipo.Equals("pdf", StringComparison.OrdinalIgnoreCase) ? "*.pdf" : "*.xml";
            var list = Directory.GetFiles(dir, patron)
                .Select(p => new FileInfo(p))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Select(f => new { nombre = f.Name, ruta = f.FullName, tamano = f.Length })
                .ToList();

            return Ok(list);
        }

        // GET api/sat/descargas/{solicitudId}/abrir?tipo=pdf|xml&nombre=archivo.ext
        [HttpGet("{solicitudId:int}/abrir")]
        public IActionResult AbrirArchivo(int solicitudId, [FromQuery] string tipo, [FromQuery] string nombre)
        {
            var d = GetUltimaDescarga(solicitudId);
            if (d == null) return NotFound("Sin descargas.");

            string? dirRel = tipo.Equals("pdf", StringComparison.OrdinalIgnoreCase)
                ? PdfDirFromExtraccion(d.carpeta_extraccion ?? "")
                : d.carpeta_extraccion;

            if (dirRel == null) return NotFound("Directorio no disponible.");

            var dir = SafeJoin(BasePath, dirRel) ?? dirRel;
            var full = Path.GetFullPath(Path.Combine(dir, nombre));

            if (!full.StartsWith(Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase))
                return BadRequest("Ruta inválida.");
            if (!System.IO.File.Exists(full))
                return NotFound("Archivo no encontrado.");

            var contentType = tipo.Equals("pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "application/xml";
            return PhysicalFile(full, contentType, enableRangeProcessing: true);
        }



        //generar pdf
        // ===============================================================
        //  GENERAR PDF DESDE XML SELECCIONADO
        // ===============================================================
        [HttpGet("{solicitudId:int}/generar-pdf")]
        public IActionResult GenerarPdfDesdeXml(int solicitudId, [FromQuery] string xml)
        {
            var d = GetUltimaDescarga(solicitudId);
            if (d == null)
                return NotFound("Sin descargas para esta solicitud.");

            if (string.IsNullOrWhiteSpace(d.carpeta_extraccion))
                return NotFound("No hay carpeta de extracción.");

            var dirXml = SafeJoin(BasePath, d.carpeta_extraccion) ?? d.carpeta_extraccion;
            var xmlFile = Path.Combine(dirXml, xml);

            if (!System.IO.File.Exists(xmlFile))
                return NotFound("El XML no existe en disco.");

            byte[] pdfBytes;

            try
            {
                pdfBytes = _satService.GenerarPDFDesdeXML_Bytes(xmlFile);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generando PDF: {ex.Message}");
            }

            return File(pdfBytes, "application/pdf", $"{Path.GetFileNameWithoutExtension(xml)}.pdf");
        }

        //[HttpGet("{solicitudId:int}/generar-pdf")]
        //public IActionResult GenerarPdfDesdeXml(int solicitudId, [FromQuery] string xml)
        //{
        //    var d = GetUltimaDescarga(solicitudId);
        //    if (d == null)
        //        return NotFound("Sin descargas para esta solicitud.");

        //    if (string.IsNullOrWhiteSpace(d.carpeta_extraccion))
        //        return NotFound("No hay carpeta de extracción.");

        //    // Carpeta de XML REAL (emitidos/recibidos)
        //    var dirXml = SafeJoin(BasePath, d.carpeta_extraccion) ?? d.carpeta_extraccion;
        //    var xmlFile = Path.Combine(dirXml, xml);

        //    if (!System.IO.File.Exists(xmlFile))
        //        return NotFound("El XML no existe en disco.");

        //    // Carpeta destino PDFs
        //    var dirPdf = Path.Combine(dirXml, "PDF");
        //    Directory.CreateDirectory(dirPdf);

        //    string pdfFile = Path.Combine(
        //        dirPdf,
        //        Path.GetFileNameWithoutExtension(xml) + ".pdf"
        //    );

        //    try
        //    {
        //        _satService.GenerarPDFDesdeXML(xmlFile, pdfFile);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest($"Error generando PDF: {ex.Message}");
        //    }

        //    // retornar PDF para vista / descarga
        //    return PhysicalFile(pdfFile, "application/pdf", Path.GetFileName(pdfFile));
        //}

    }
}
