using API_asemp.Models.BD;
using ASEMP.CFDI.DescargaMasiva.Constants;
using ASEMP.CFDI.DescargaMasiva.Helpers;
using ASEMP.CFDI.DescargaMasiva.Interfaces;
using ASEMP.CFDI.DescargaMasiva.Models;
using Microsoft.Extensions.Configuration;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace API_asemp.Servicios
{
    public class SatService
    {
        private readonly IAutenticacionService _autenticacion;
        private readonly ISolicitudService _solicitud;
        private readonly IVerificacionService _verificacion;
        private readonly IDescargaService _descarga;
        private readonly IConfiguration _config;

        private readonly IHttpSoapClient _soap;


        public SatService(
        IAutenticacionService autenticacion,
        ISolicitudService solicitud,
        IVerificacionService verificacion, IHttpSoapClient soap,
        IDescargaService descarga,
        IConfiguration config)
        {
            _autenticacion = autenticacion;
            _solicitud = solicitud;
            _verificacion = verificacion;
            _descarga = descarga;
            _soap = soap;
            _config = config;

            QuestPDF.Settings.License = LicenseType.Community;
        }

        // =====================================================
        // 🔹 Autenticación directa
        // =====================================================
        public async Task<(bool Ok, string Token)> AutenticacionAsync(X509Certificate2 cert)
        {
            var authReq = AutenticacionRequest.CreateInstance();
            var result = await _autenticacion.SendSoapRequestAsync(authReq, cert, CancellationToken.None);

            if (!result.AccessToken.IsValid)
                return (false, "");

            return (true, result.AccessToken.DecodedValue);
        }

        public string GetStoragePath()
        {
            return _config["SatStoragePath"] ?? "C:\\API_asemp_data\\SAT";

            //var projectPath = AppDomain.CurrentDomain.BaseDirectory;
            //var defaultPath = Path.Combine(projectPath, "uploads", "SAT");
            //// Crear directorio si no existe
            //Directory.CreateDirectory(defaultPath);
            //return defaultPath;
        }
        // =====================================================
        // 🔹 Enviar solicitud de descarga masiva
        // =====================================================

        //public async Task<SatSolicitudResultado> EnviarSolicitudAsync(SolicitudSAT solicitud, CertificadoSAT certificado)
        //{
        //    CancellationToken ct = CancellationToken.None;
        //    string basePath = GetStoragePath();

        //    // --------------------------------------
        //    // 1) Cargar certificado
        //    // --------------------------------------
        //    string rutaPfx = certificado.archivo_pfx!;
        //    if (!Path.IsPathRooted(rutaPfx))
        //        rutaPfx = Path.Combine(basePath, rutaPfx);
        //    if (!File.Exists(rutaPfx))
        //        throw new FileNotFoundException($"No se encontró el archivo PFX en: {rutaPfx}");

        //    byte[] pfxBytes = File.ReadAllBytes(rutaPfx);
        //    var cert = X509Certificate2Helper.GetCertificate(pfxBytes, certificado.contrasena!);

        //    // --------------------------------------
        //    // 2) Autenticación SAT
        //    // --------------------------------------
        //    var authReq = AutenticacionRequest.CreateInstance();
        //    var authResult = await _autenticacion.SendSoapRequestAsync(authReq, cert, ct);

        //    if (!authResult.AccessToken.IsValid)
        //    {
        //        return new SatSolicitudResultado
        //        {
        //            EstadoSolicitud = 0,
        //            CodigoEstado = authResult.FaultCode,
        //            Mensaje = $"Error autenticando: {authResult.FaultString}"
        //        };
        //    }

        //    var token = authResult.AccessToken;
        //    var rfcSolicitante = certificado.rfc!;

        //    // --------------------------------------
        //    // 3) Crear SolicitudRequest normal
        //    // --------------------------------------
        //    SolicitudRequest solicitudSAT =
        //        solicitud.tipo_solicitud?.ToLower() == "recibidos"
        //        ? SolicitudRequest.CreateRecibidos(solicitud.fecha_inicio, solicitud.fecha_fin, rfcSolicitante, token)
        //        : SolicitudRequest.CreateEmitidos(solicitud.fecha_inicio, solicitud.fecha_fin, rfcSolicitante, null, rfcSolicitante, token);

        //    // --------------------------------------
        //    // 4) Generar XML original del DLL
        //    // --------------------------------------
        //    string xmlOriginal = _solicitud.GenerateSoapRequestEnvelopeXmlContent(solicitudSAT, cert);

        //    // --------------------------------------
        //    // 5) Insertar EstadoComprobante="Vigente" sin romper firma
        //    // --------------------------------------
        //    var xmlDoc = new XmlDocument();
        //    xmlDoc.LoadXml(xmlOriginal);

        //    XmlNamespaceManager ns = new XmlNamespaceManager(xmlDoc.NameTable);
        //    ns.AddNamespace("s11", "http://schemas.xmlsoap.org/soap/envelope/");
        //    ns.AddNamespace("des", "http://www.sat.gob.mx/esquemas/descarga");

        //    // nodo correcto para la versión 1.0.0
        //    var nodoSolicitud = xmlDoc.SelectSingleNode("//des:SolicitaDescarga", ns)
        //                         ?? xmlDoc.SelectSingleNode("//des:solicitud", ns);

        //    if (nodoSolicitud != null)
        //    {
        //        XmlAttribute attr = xmlDoc.CreateAttribute("EstadoComprobante");
        //        attr.Value = "Vigente";
        //        nodoSolicitud.Attributes.Append(attr);
        //    }

        //    string xmlFinal = xmlDoc.OuterXml;

        //    // --------------------------------------
        //    // 6) Enviar solicitud al SAT
        //    // --------------------------------------
        //    var soapResult = await _soap.SendRequestAsync(
        //        CfdiDescargaMasivaWebServiceUrls.SolicitudUrl,
        //        solicitud.tipo_solicitud?.ToLower() == "recibidos"
        //            ? CfdiDescargaMasivaWebServiceUrls.SolicitudRecibidosSoapActionUrl
        //            : CfdiDescargaMasivaWebServiceUrls.SolicitudEmitidosSoapActionUrl,
        //        token,
        //        xmlFinal,
        //        ct
        //    );

        //    var res = _solicitud.GetSoapResponseResult(soapResult);

        //    return new SatSolicitudResultado
        //    {
        //        EstadoSolicitud = string.IsNullOrEmpty(res.RequestId) ? 0 : 1,
        //        CodigoEstado = res.RequestStatusCode,
        //        Mensaje = res.RequestStatusMessage,
        //        Token = res.RequestId ?? ""
        //    };
        //}
        public async Task<SatService.SatSolicitudResultado> EnviarSolicitudAsync(
     SolicitudSAT solicitud,
     CertificadoSAT certificado)
        {
            CancellationToken ct = CancellationToken.None;
            string basePath = GetStoragePath();

            // 1) Cargar certificado PFX
            string rutaPfx = certificado.archivo_pfx!;
            if (!Path.IsPathRooted(rutaPfx))
                rutaPfx = Path.Combine(basePath, rutaPfx);
            if (!File.Exists(rutaPfx))
                throw new FileNotFoundException($"No se encontró el archivo PFX en: {rutaPfx}");

            byte[] pfxBytes = File.ReadAllBytes(rutaPfx);
            var cert = X509Certificate2Helper.GetCertificate(pfxBytes, certificado.contrasena!);

            // 2) Autenticación SAT
            var authReq = AutenticacionRequest.CreateInstance();
            var authResult = await _autenticacion.SendSoapRequestAsync(authReq, cert, ct);

            if (!authResult.AccessToken.IsValid)
            {
                return new SatSolicitudResultado
                {
                    EstadoSolicitud = 0,
                    CodigoEstado = authResult.FaultCode,
                    Mensaje = $"Error autenticando: {authResult.FaultString}"
                };
            }

            var token = authResult.AccessToken;
            var rfcSolicitante = certificado.rfc!;

            // 3) Construir SolicitudRequest IGUAL que en la consola
            SolicitudRequest solicitudSAT;
            var tipo = solicitud.tipo_solicitud?.ToLowerInvariant();

            if (tipo == "recibidos")
            {
                solicitudSAT = SolicitudRequest.CreateRecibidos(
                    startDate: solicitud.fecha_inicio,
                    endDate: solicitud.fecha_fin,
                    requestingRfc: rfcSolicitante,   // RFC del cliente
                    accessToken: token
                );
            }
            else if (tipo == "emitidos")
            {
                solicitudSAT = SolicitudRequest.CreateEmitidos(
                    startDate: solicitud.fecha_inicio,
                    endDate: solicitud.fecha_fin,
                    senderRfc: rfcSolicitante,      // mismo RFC como emisor
                    recipientsRfcs: null,            // sin filtro de receptor
                    requestingRfc: rfcSolicitante,
                    accessToken: token
                );
            }
            else
            {
                return new SatSolicitudResultado
                {
                    EstadoSolicitud = 0,
                    CodigoEstado = "CLIENT",
                    Mensaje = "Tipo de solicitud inválido. Debe ser 'recibidos' o 'emitidos'."
                };
            }

            // 4) (Opcional) ver XML que se va a enviar – solo para depurar
            string xmlDebug = _solicitud.GenerateSoapRequestEnvelopeXmlContent(solicitudSAT, cert);
            Console.WriteLine("===== XML ANTES DE ENVIAR DESDE API =====");
            Console.WriteLine(xmlDebug);

            // 5) Enviar usando la DLL (igual que en la consola)
            var res = await _solicitud.SendSoapRequestAsync(solicitudSAT, cert, ct);

            // 6) Mapear al resultado de tu servicio
            return new SatSolicitudResultado
            {
                // tú estabas usando 0/1 para saber si generó RequestId
                EstadoSolicitud = string.IsNullOrEmpty(res.RequestId) ? 0 : 1,
                CodigoEstado = res.RequestStatusCode,
                Mensaje = res.RequestStatusMessage,
                Token = res.RequestId ?? ""
            };
        }




        //public async Task<SatSolicitudResultado> EnviarSolicitudAsync(SolicitudSAT solicitud, CertificadoSAT certificado)
        //{
        //    CancellationToken ct = CancellationToken.None;
        //    string basePath = GetStoragePath();

        //    // ✅ Construir ruta del PFX correctamente
        //    string rutaPfx = certificado.archivo_pfx!;
        //    if (!Path.IsPathRooted(rutaPfx))
        //        rutaPfx = Path.Combine(basePath, rutaPfx);
        //    if (!File.Exists(rutaPfx))
        //        throw new FileNotFoundException($"No se encontró el archivo PFX en: {rutaPfx}");

        //    byte[] pfxBytes = File.ReadAllBytes(rutaPfx);
        //    var cert = X509Certificate2Helper.GetCertificate(pfxBytes, certificado.contrasena!);

        //    // 🔹 Autenticación SAT
        //    var authReq = AutenticacionRequest.CreateInstance();
        //    var authResult = await _autenticacion.SendSoapRequestAsync(authReq, cert, ct);
        //    if (!authResult.AccessToken.IsValid)
        //    {
        //        return new SatSolicitudResultado
        //        {
        //            EstadoSolicitud = 0,
        //            CodigoEstado = authResult.FaultCode,
        //            Mensaje = $"Error autenticando: {authResult.FaultString}"
        //        };
        //    }

        //    var token = authResult.AccessToken;
        //    var rfcSolicitante = certificado.rfc!;
        //    SolicitudRequest solicitudSAT =
        //    (solicitud.tipo_solicitud?.ToLower() == "recibidos")
        //    ? SolicitudRequest.CreateRecibidos(solicitud.fecha_inicio, solicitud.fecha_fin, rfcSolicitante, token)
        //    : SolicitudRequest.CreateEmitidos(solicitud.fecha_inicio, solicitud.fecha_fin, rfcSolicitante, null, rfcSolicitante, token);

        //    // 🔥 ARREGLO OBLIGATORIO
        //    solicitudSAT.EstadoComprobante = "Vigente";


        //    var result = await _solicitud.SendSoapRequestAsync(solicitudSAT, cert, ct);

        //    return new SatSolicitudResultado
        //    {
        //        EstadoSolicitud = string.IsNullOrEmpty(result.RequestId) ? 0 :
        //    result.RequestStatusMessage.Contains("terminada", StringComparison.OrdinalIgnoreCase) ? 3 : 1,
        //        CodigoEstado = result.RequestStatusCode,
        //        Mensaje = result.RequestStatusMessage,
        //        Token = result.RequestId ?? ""
        //    };
        //}

        // =====================================================
        // 🔹 Verificar estado de una solicitud
        // =====================================================
        public async Task<SatVerificacionResultado> VerificarSolicitudAsync(SolicitudSAT solicitud, CertificadoSAT certificado)
        {
            CancellationToken ct = CancellationToken.None;
            string basePath = GetStoragePath();

            // ✅ Validar ruta del PFX
            string rutaPfx = certificado.archivo_pfx!;
            if (!Path.IsPathRooted(rutaPfx))
                rutaPfx = Path.Combine(basePath, rutaPfx);
            if (!File.Exists(rutaPfx))
                throw new FileNotFoundException($"No se encontró el archivo PFX en: {rutaPfx}");

            byte[] pfxBytes = File.ReadAllBytes(rutaPfx);
            var cert = X509Certificate2Helper.GetCertificate(pfxBytes, certificado.contrasena!);

            var authReq = AutenticacionRequest.CreateInstance();
            var authResult = await _autenticacion.SendSoapRequestAsync(authReq, cert, ct);
            if (!authResult.AccessToken.IsValid)
            {
                return new SatVerificacionResultado
                {
                    EstadoSolicitud = 0,
                    CodigoEstado = authResult.FaultCode,
                    Mensaje = "Error autenticando con el SAT",
                    NumeroCfdis = 0
                };
            }

            var token = authResult.AccessToken;
            var verifReq = VerificacionRequest.CreateInstance(solicitud.token!, certificado.rfc!, token);
            var verifResult = await _verificacion.SendSoapRequestAsync(verifReq, cert, ct);

            return new SatVerificacionResultado
            {
                EstadoSolicitud = int.TryParse(verifResult.DownloadRequestStatusNumber, out var est) ? est : 0,
                CodigoEstado = verifResult.DownloadRequestStatusCode ?? "",
                Mensaje = verifResult.RequestStatusMessage ?? "",
                NumeroCfdis = int.TryParse(verifResult.NumberOfCfdis, out var n) ? n : 0
            };
        }









        //
        private (int Anio, int Mes) ObtenerFechaDesdeXml(string xmlFile)
{
    var xml = XDocument.Load(xmlFile);
    var comprobante = xml.Root;

    string fechaStr = comprobante?.Attribute("Fecha")?.Value
                      ?? comprobante?.Attribute("fecha")?.Value
                      ?? throw new Exception($"No existe atributo Fecha en {xmlFile}");

    var fecha = DateTime.Parse(fechaStr);
    return (fecha.Year, fecha.Month);
}

private string NombreMes(int mes)
{
    string[] meses =
    {
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    };
    return meses[mes - 1];
}




        private bool EsCfdiEmitido(string xmlFile, string rfcCertificado)
        {
            try
            {
                var xml = XDocument.Load(xmlFile);
                XNamespace cfdi = xml.Root!.Name.Namespace;

                var emisor = xml.Root.Element(cfdi + "Emisor");
                var receptor = xml.Root.Element(cfdi + "Receptor");

                string rfcEmisor = emisor?.Attribute("Rfc")?.Value ?? "";
                string rfcReceptor = receptor?.Attribute("Rfc")?.Value ?? "";

                return rfcEmisor.Equals(rfcCertificado, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }


        private string ObtenerRutaDestino(int clienteId, string rfc, int anio, int mes, bool emitidos)
        {
            string basePath = GetStoragePath();

            string ruta = Path.Combine(
                basePath,
                "Clientes",
                $"{clienteId}_{rfc}",
                anio.ToString(),
                NombreMes(mes),
                emitidos ? "emitidos" : "recibidos"
            );

            Directory.CreateDirectory(ruta);
            return ruta;
        }


        public async Task<SatDescargaResultado> DescargarPaqueteAsync(
      string requestId,
      CertificadoSAT certificado,
      AccessToken token,
      string tipoSolicitud
  )
        {
            CancellationToken ct = CancellationToken.None;
            string basePath = GetStoragePath();

            // 1) Validar PFX
            string rutaPfx = certificado.archivo_pfx!;
            if (!Path.IsPathRooted(rutaPfx))
                rutaPfx = Path.Combine(basePath, rutaPfx);
            if (!File.Exists(rutaPfx))
                throw new FileNotFoundException($"No se encontró el archivo PFX en: {rutaPfx}");

            var cert = X509Certificate2Helper.GetCertificate(
                File.ReadAllBytes(rutaPfx),
                certificado.contrasena!
            );

            // 2) Verificar solicitud SAT
            var verifReq = VerificacionRequest.CreateInstance(requestId, certificado.rfc!, token);
            var verifRes = await _verificacion.SendSoapRequestAsync(verifReq, cert, ct);

            var paquetes = verifRes.PackageIds?.ToList() ?? new List<string>();
            if (paquetes.Count == 0)
            {
                return new SatDescargaResultado
                {
                    Estado = "Error",
                    Mensaje = "No hay paquetes disponibles para descargar."
                };
            }

            // 3) Carpeta temporal
            string temp = Path.Combine(basePath, "Temp", Guid.NewGuid().ToString());
            string tempZip = Path.Combine(temp, "ZIP");
            string tempXml = Path.Combine(temp, "XML");
            Directory.CreateDirectory(tempZip);
            Directory.CreateDirectory(tempXml);

            List<string> listaZipDescargados = new();

            // 4) Descargar ZIPs y extraer XML
            foreach (var pkg in paquetes)
            {
                var descargaReq = DescargaRequest.CreateInstace(pkg, certificado.rfc!, token);
                var descargaRes = await _descarga.SendSoapRequestAsync(descargaReq, cert, ct);

                if (string.IsNullOrWhiteSpace(descargaRes.Package))
                    continue;

                string zipTempPath = Path.Combine(tempZip, $"{pkg}.zip");
                File.WriteAllBytes(zipTempPath, Convert.FromBase64String(descargaRes.Package));
                listaZipDescargados.Add(zipTempPath);

                ZipFile.ExtractToDirectory(zipTempPath, tempXml, true);
            }

            // 5) Emitidos / Recibidos (según el usuario)
            //aqui tenia un uno rojo para debug
            bool solicitudEmitidos = tipoSolicitud.ToLower() == "emitidos";

            // Ruta final (la primera que se procesará)
            string? carpetaFinal = null;
            List<string> rutasZipFinales = new();

            // 6) Procesar XML y colocar ZIP en carpeta real
            foreach (var xmlFile in Directory.GetFiles(tempXml, "*.xml"))
            {
                var (anio, mes) = ObtenerFechaDesdeXml(xmlFile);

                // Checar si el XML realmente es emitido
                bool xmlEmitido = EsCfdiEmitido(xmlFile, certificado.rfc!);

                // Regla final
                bool esEmitido = solicitudEmitidos ? true : xmlEmitido;

                string destino = ObtenerRutaDestino(
                    certificado.cliente_id,
                    certificado.rfc!,
                    anio,
                    mes,
                    esEmitido
                );

                Directory.CreateDirectory(destino);

                if (carpetaFinal == null)
                    carpetaFinal = destino;

                // Copiar XML
                string xmlDestino = Path.Combine(destino, Path.GetFileName(xmlFile));
                File.Copy(xmlFile, xmlDestino, true);
            }

            // 7) Guardar ZIP también en la carpeta emitidos/recibidos
            if (carpetaFinal != null)
            {
                foreach (var zipTempFile in listaZipDescargados)
                {
                    string zipFinal = Path.Combine(carpetaFinal, Path.GetFileName(zipTempFile));
                    File.Copy(zipTempFile, zipFinal, true);
                    rutasZipFinales.Add(zipFinal); // aquí siguen siendo absolutas
                }
            }

            // 8) Limpiar TEMP
            try { Directory.Delete(temp, true); } catch { }

            // 9) Convertir rutas absolutas a rutas RELATIVAS antes de regresar
            string archivoZipRel = string.Join(",",
                rutasZipFinales.Select(z => ToRelativeSatPath(z)));

            string carpetaFinalRel = carpetaFinal != null
                ? ToRelativeSatPath(carpetaFinal)
                : "";

            return new SatDescargaResultado
            {
                Estado = "OK",
                Mensaje = $"Se procesaron {paquetes.Count} paquetes.",
                ArchivoZip = archivoZipRel,   // ← ahora relativo
                CarpetaXml = carpetaFinalRel, // ← ahora relativo
                CarpetaPdf = carpetaFinalRel  // ← ahora relativo
            };

            //// 7) Guardar ZIP también en la carpeta emitidos/recibidos
            //if (carpetaFinal != null)
            //{
            //    foreach (var zipTempFile in listaZipDescargados)
            //    {
            //        string zipFinal = Path.Combine(carpetaFinal, Path.GetFileName(zipTempFile));
            //        File.Copy(zipTempFile, zipFinal, true);
            //        rutasZipFinales.Add(zipFinal); // guarda zip real
            //    }
            //}

            //// 8) Limpiar TEMP
            //try { Directory.Delete(temp, true); } catch { }

            //// 9) Respuesta final real
            //return new SatDescargaResultado
            //{
            //    Estado = "OK",
            //    Mensaje = $"Se procesaron {paquetes.Count} paquetes.",
            //    ArchivoZip = string.Join(",", rutasZipFinales),   // ZIP reales
            //    CarpetaXml = carpetaFinal,                       // Carpeta REAL
            //    CarpetaPdf = carpetaFinal                        // PDF no se genera, pero dejamos campo igual
            //};
        }


        private string ToRelativeSatPath(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
                return absolutePath;

            // Carpeta base donde empieza todo (C:\API_asemp_data\SAT)
            string basePath = Path.GetFullPath(
                GetStoragePath().TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));

            string full = Path.GetFullPath(absolutePath);

            if (!full.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                return absolutePath; // por si viene algo fuera de SatStoragePath

            string relative = full.Substring(basePath.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return relative.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }




        //        public async Task<SatDescargaResultado> DescargarPaqueteAsync(
        //    string requestId,
        //    CertificadoSAT certificado,
        //    AccessToken token,
        //    string tipoSolicitud   // “emitidos” | “recibidos”
        //)
        //        {
        //            CancellationToken ct = CancellationToken.None;
        //            string basePath = GetStoragePath();

        //            // 1) Validar PFX
        //            string rutaPfx = certificado.archivo_pfx!;
        //            if (!Path.IsPathRooted(rutaPfx))
        //                rutaPfx = Path.Combine(basePath, rutaPfx);

        //            if (!File.Exists(rutaPfx))
        //                throw new FileNotFoundException($"No se encontró el archivo PFX en: {rutaPfx}");

        //            var cert = X509Certificate2Helper.GetCertificate(
        //                File.ReadAllBytes(rutaPfx),
        //                certificado.contrasena!
        //            );

        //            // 2) Verificar solicitud SAT
        //            var verifReq = VerificacionRequest.CreateInstance(requestId, certificado.rfc!, token);
        //            var verifRes = await _verificacion.SendSoapRequestAsync(verifReq, cert, ct);

        //            var paquetes = verifRes.PackageIds?.ToList() ?? new List<string>();
        //            if (paquetes.Count == 0)
        //            {
        //                return new SatDescargaResultado
        //                {
        //                    Estado = "Error",
        //                    Mensaje = "No hay paquetes disponibles para descargar."
        //                };
        //            }

        //            // 3) Carpeta temporal
        //            string temp = Path.Combine(basePath, "Temp", Guid.NewGuid().ToString());
        //            string tempZip = Path.Combine(temp, "ZIP");
        //            string tempXml = Path.Combine(temp, "XML");
        //            Directory.CreateDirectory(tempZip);
        //            Directory.CreateDirectory(tempXml);

        //            // 4) Descargar ZIPs
        //            foreach (var pkg in paquetes)
        //            {
        //                var descargaReq = DescargaRequest.CreateInstace(pkg, certificado.rfc!, token);
        //                var descargaRes = await _descarga.SendSoapRequestAsync(descargaReq, cert, ct);

        //                if (string.IsNullOrWhiteSpace(descargaRes.Package))
        //                    continue;

        //                string zipPath = Path.Combine(tempZip, $"{pkg}.zip");
        //                File.WriteAllBytes(zipPath, Convert.FromBase64String(descargaRes.Package));

        //                ZipFile.ExtractToDirectory(zipPath, tempXml, true);
        //            }


        //            // 5) Emitidos / Recibidos (lo que viene del usuario)
        //            bool solicitudEmitidos = tipoSolicitud.ToLower() == "emitidos";

        //            // Carpeta final que devolveremos al controller
        //            string? carpetaFinal = null;

        //            // 6) Mover XML a destino definitivo (NO generar PDF)
        //            foreach (var xml in Directory.GetFiles(tempXml, "*.xml"))
        //            {
        //                var (anio, mes) = ObtenerFechaDesdeXml(xml);

        //                // Checar si realmente es emitido según el XML
        //                bool xmlEmitido = EsCfdiEmitido(xml, certificado.rfc!);

        //                // Regla final:
        //                // - Si el usuario dijo emitidos → se van a emitidos
        //                // - Si dijo recibidos → se van a recibidos
        //                // - Pero si quieres que el XML corrija errores, puedes usar:
        //                //   bool esEmitido = xmlEmitido;
        //                bool esEmitido = solicitudEmitidos ? true : xmlEmitido;

        //                string destino = ObtenerRutaDestino(
        //                    certificado.cliente_id,
        //                    certificado.rfc!,
        //                    anio,
        //                    mes,
        //                    esEmitido
        //                );

        //                Directory.CreateDirectory(destino);

        //                if (carpetaFinal == null)
        //                    carpetaFinal = destino;

        //                // Copiar XML (sin PDF)
        //                File.Copy(xml, Path.Combine(destino, Path.GetFileName(xml)), true);
        //            }

        //            // 7) Limpiar TEMP
        //            try { Directory.Delete(temp, true); } catch { }

        //            // 8) Respuesta REAL
        //            return new SatDescargaResultado
        //            {
        //                Estado = "OK",
        //                Mensaje = $"Se procesaron {paquetes.Count} paquetes.",

        //                // YA NO regresamos rutas temporales
        //                ArchivoZip = "",

        //                // Ruta REAL donde quedaron los XML
        //                CarpetaXml = carpetaFinal,

        //                // Misma carpeta (no generamos PDF)
        //                CarpetaPdf = carpetaFinal
        //            };
        //        }





        //// =====================================================
        //// 🔹 Descargar, extraer y generar PDF (flujo correcto SAT)
        //// =====================================================
        //public async Task<SatDescargaResultado> DescargarPaqueteAsync(
        //string requestId,
        //CertificadoSAT certificado,
        //ASEMP.CFDI.DescargaMasiva.Models.AccessToken token)
        //{
        //    CancellationToken ct = CancellationToken.None;
        //    string basePath = GetStoragePath();

        //    // 1️⃣ Validar PFX
        //    string rutaPfx = certificado.archivo_pfx!;
        //    if (!Path.IsPathRooted(rutaPfx))
        //        rutaPfx = Path.Combine(basePath, rutaPfx);
        //    if (!File.Exists(rutaPfx))
        //        throw new FileNotFoundException($"No se encontró el archivo PFX en: {rutaPfx}");

        //    byte[] pfxBytes = File.ReadAllBytes(rutaPfx);
        //    var cert = X509Certificate2Helper.GetCertificate(pfxBytes, certificado.contrasena!);

        //    // 2️⃣ Verificar solicitud para obtener los paquetes reales
        //    var verifReq = VerificacionRequest.CreateInstance(requestId, certificado.rfc!, token);
        //    var verifRes = await _verificacion.SendSoapRequestAsync(verifReq, cert, ct);

        //    var paquetes = verifRes.PackageIds?.ToList() ?? new List<string>();
        //    if (paquetes.Count == 0)
        //    {
        //        return new SatDescargaResultado
        //        {
        //            Estado = "Error",
        //            Mensaje = "No hay paquetes disponibles para descargar. Verifica que la solicitud esté en estado 'Terminada' (3, 5000)."
        //        };
        //    }

        //    Console.WriteLine($"📦 Paquetes detectados: {string.Join(", ", paquetes)}");

        //    // 3️⃣ Crear carpetas de destino
        //    string carpetaRFC = Path.Combine(basePath, "Clientes", $"{certificado.cliente_id}_{certificado.rfc!}");
        //    string carpetaDia = Path.Combine(carpetaRFC, DateTime.UtcNow.ToString("yyyyMMdd"));
        //    string carpetaZip = Path.Combine(carpetaDia, "ZIP");
        //    string carpetaXml = Path.Combine(carpetaDia, "XML");
        //    string carpetaPdf = Path.Combine(carpetaDia, "PDF");
        //    Directory.CreateDirectory(carpetaZip);
        //    Directory.CreateDirectory(carpetaXml);
        //    Directory.CreateDirectory(carpetaPdf);

        //    List<string> archivosZip = new();

        //    // 4️⃣ Descargar cada paquete
        //    foreach (var pkg in paquetes)
        //    {
        //        Console.WriteLine($"🎯 Descargando paquete: {pkg}");
        //        var descargaReq = DescargaRequest.CreateInstace(pkg, certificado.rfc!, token);
        //        var descargaRes = await _descarga.SendSoapRequestAsync(descargaReq, cert, ct);

        //        if (string.IsNullOrWhiteSpace(descargaRes.Package))
        //        {
        //            Console.WriteLine($"❌ Paquete vacío o no disponible: {pkg}");
        //            continue;
        //        }

        //        byte[] zipBytes = Convert.FromBase64String(descargaRes.Package);
        //        string zipPath = Path.Combine(carpetaZip, $"{pkg}.zip");
        //        await File.WriteAllBytesAsync(zipPath, zipBytes);
        //        archivosZip.Add(zipPath);

        //        try
        //        {
        //            ZipFile.ExtractToDirectory(zipPath, carpetaXml, overwriteFiles: true);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"⚠️ Error extrayendo {pkg}: {ex.Message}");
        //        }
        //    }

        //    // 5️⃣ Generar PDFs desde los XML descargados
        //    foreach (var xmlFile in Directory.GetFiles(carpetaXml, "*.xml"))
        //    {
        //        try
        //        {
        //            string pdfFile = Path.Combine(carpetaPdf, Path.GetFileNameWithoutExtension(xmlFile) + ".pdf");
        //            GenerarPDFDesdeXML(xmlFile, pdfFile);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error generando PDF: {ex.Message}");
        //        }
        //    }

        //    // 6️⃣ Resultado final
        //    return new SatDescargaResultado
        //    {
        //        Estado = archivosZip.Count > 0 ? "OK" : "Error",
        //        Mensaje = archivosZip.Count > 0
        //    ? $"Se descargaron {archivosZip.Count} paquetes correctamente."
        //    : "No se descargó ningún paquete válido.",
        //        ArchivoZip = string.Join(",", archivosZip),
        //        CarpetaXml = carpetaXml,
        //        CarpetaPdf = carpetaPdf
        //    };
        //}



        // =====================================================
        // 🔹 Generar PDF desde XML
        // 🔹 Generar PDF desde XML (Diseño oficial SAT adaptado)
        // =====================================================
        // ======= FUNCIÓN: Generar PDF desde XML (Diseño oficial SAT completo) =======
        // =====================================================
        // 🔹 Generar PDF desde XML (Diseño oficial SAT adaptado)
        // =====================================================
        // ======= FUNCIÓN: Generar PDF desde XML (Diseño oficial SAT completo) =======
        public void GenerarPDFDesdeXML(string rutaXml, string rutaPdf)
        {
            try
            {
                XDocument xml = XDocument.Load(rutaXml);
                XNamespace cfdi = xml.Root!.Name.Namespace;
                XNamespace tfd = "http://www.sat.gob.mx/TimbreFiscalDigital";

                var comp = xml.Element(cfdi + "Comprobante");
                var emisor = comp?.Element(cfdi + "Emisor");
                var receptor = comp?.Element(cfdi + "Receptor");
                var timbre = comp?.Element(cfdi + "Complemento")?.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "TimbreFiscalDigital");
                var conceptos = comp?.Element(cfdi + "Conceptos")?.Elements(cfdi + "Concepto") ?? Enumerable.Empty<XElement>();

                // ===== Datos base =====
                string uuid = timbre?.Attribute("UUID")?.Value ?? "";
                string total = comp?.Attribute("Total")?.Value ?? "0.00";
                string subtotal = comp?.Attribute("SubTotal")?.Value ?? "0.00";
                string fecha = comp?.Attribute("Fecha")?.Value ?? "";
                string metodo = comp?.Attribute("MetodoPago")?.Value ?? "";
                string forma = comp?.Attribute("FormaPago")?.Value ?? "";
                string moneda = comp?.Attribute("Moneda")?.Value ?? "";
                string lugar = comp?.Attribute("LugarExpedicion")?.Value ?? "";
                string tipoComprobante = comp?.Attribute("TipoDeComprobante")?.Value ?? "";
                string exportacion = comp?.Attribute("Exportacion")?.Value ?? "";
                string noCertificado = comp?.Attribute("NoCertificado")?.Value ?? "";
                string rfcEmisor = emisor?.Attribute("Rfc")?.Value ?? "";
                string nombreEmisor = emisor?.Attribute("Nombre")?.Value ?? "";
                string regimenEmisor = emisor?.Attribute("RegimenFiscal")?.Value ?? "";
                string rfcReceptor = receptor?.Attribute("Rfc")?.Value ?? "";
                string nombreReceptor = receptor?.Attribute("Nombre")?.Value ?? "";
                string regimenReceptor = receptor?.Attribute("RegimenFiscalReceptor")?.Value ?? "";
                string usoCfdi = receptor?.Attribute("UsoCFDI")?.Value ?? "";
                string codPostalReceptor = receptor?.Attribute("DomicilioFiscalReceptor")?.Value ?? "";
                string selloCFDI = comp?.Attribute("Sello")?.Value ?? "";
                string selloSAT = timbre?.Attribute("SelloSAT")?.Value ?? "";
                string fechaTimbrado = timbre?.Attribute("FechaTimbrado")?.Value ?? "";
                string noCertSAT = timbre?.Attribute("NoCertificadoSAT")?.Value ?? "";

                // ===== QR =====
                string fe = selloCFDI.Length >= 8 ? selloCFDI[^8..] : "";
                string totalQR = decimal.Parse(total, CultureInfo.InvariantCulture).ToString("0.000000", CultureInfo.InvariantCulture);
                string urlQR = $"https://verificacfdi.facturaelectronica.sat.gob.mx/default.aspx?id={uuid}&re={rfcEmisor}&rr={rfcReceptor}&tt={totalQR}&fe={fe}";

                byte[] qrBytes;
                using (var qrGen = new QRCoder.QRCodeGenerator())
                {
                    var qrData = qrGen.CreateQrCode(urlQR, QRCoder.QRCodeGenerator.ECCLevel.Q);
                    var qrCode = new QRCoder.PngByteQRCode(qrData);
                    qrBytes = qrCode.GetGraphic(4);
                }

                string logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "LogoSAT.png");

                // ===== Crear PDF =====
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(25);
                        page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Arial"));

                        // === ENCABEZADO ===
                        page.Header().PaddingBottom(10).Row(row =>
                        {
                            if (File.Exists(logoPath))
                                row.ConstantItem(120).Height(60).Image(logoPath).FitWidth();
                            else
                                row.ConstantItem(120).Height(60).Text("SAT").Bold().FontSize(14);

                            row.RelativeItem().AlignMiddle().AlignCenter()
                                .Text("COMPROBANTE FISCAL DIGITAL POR INTERNET (CFDI 4.0)")
                                .Bold().FontSize(14);
                        });

                        // === CONTENIDO PRINCIPAL ===
                        page.Content().Column(col =>
                        {
                            col.Spacing(10);

                            // === BLOQUE DATOS GENERALES ===
                            static string MapRegimen(string c) => c switch
                            {
                                "626" => "Régimen Simplificado de Confianza",
                                "603" => "Personas Morales con Fines no Lucrativos",
                                _ => c ?? "-"
                            };

                            static string MapUsoCfdi(string c) => c switch
                            {
                                "G03" => "Gastos en general",
                                "D01" => "Honorarios médicos, dentales y hospitalarios",
                                "P01" => "Por definir",
                                _ => c ?? "-"
                            };

                            col.Item().PaddingBottom(5).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(1.5f);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(1.5f);
                                });

                                void AddRow(string leftLabel, string leftValue, string rightLabel, string rightValue)
                                {
                                    table.Cell().Text(leftLabel).Bold();
                                    table.Cell().Text(leftValue);
                                    table.Cell().Text(rightLabel).Bold();
                                    table.Cell().Text(rightValue);
                                }

                                AddRow("RFC emisor:", rfcEmisor, "Folio fiscal:", uuid);
                                AddRow("Nombre emisor:", nombreEmisor, "No. de serie del CSD:", noCertificado);
                                AddRow("RFC receptor:", rfcReceptor, "Código postal / Fecha emisión:", $"{lugar} {fecha}");
                                AddRow("Nombre receptor:", nombreReceptor, "Efecto de comprobante:", tipoComprobante == "I" ? "Ingreso" : tipoComprobante);
                                AddRow("Código postal del receptor:", codPostalReceptor, "Régimen fiscal:", MapRegimen(regimenEmisor));
                                AddRow("Régimen fiscal receptor:", MapRegimen(regimenReceptor), "Exportación:", exportacion == "01" ? "No aplica" : exportacion);
                                AddRow("Uso CFDI:", MapUsoCfdi(usoCfdi), "", "");
                            });

                            // === TÍTULO CONCEPTOS ===
                            col.Item().PaddingTop(10).Text("Conceptos").Bold().FontSize(10);

                            // === TABLA DE CONCEPTOS ===
                            col.Item().Table(table =>
                            {
                                string gray = "#E6E6E6";
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(70);
                                    cols.ConstantColumn(65);
                                    cols.ConstantColumn(55);
                                    cols.ConstantColumn(55);
                                    cols.ConstantColumn(70);
                                    cols.ConstantColumn(70);
                                    cols.ConstantColumn(70);
                                    cols.ConstantColumn(90);
                                });

                                table.Header(h =>
                                {
                                    void HC(string t) => h.Cell().Background(gray).Border(0.5f).Padding(2).Text(t).Bold().FontSize(7);
                                    HC("Clave del producto y/o servicio");
                                    HC("No. identificación");
                                    HC("Cantidad");
                                    HC("Clave de unidad");
                                    HC("Unidad");
                                    HC("Valor unitario");
                                    HC("Importe");
                                    HC("Objeto impuesto");
                                });

                                foreach (var c in conceptos)
                                {
                                    table.Cell().Border(0.5f).Padding(2).Text(c.Attribute("ClaveProdServ")?.Value ?? "");
                                    table.Cell().Border(0.5f).Padding(2).Text(c.Attribute("NoIdentificacion")?.Value ?? "");
                                    table.Cell().Border(0.5f).Padding(2).Text(c.Attribute("Cantidad")?.Value ?? "");
                                    table.Cell().Border(0.5f).Padding(2).Text(c.Attribute("ClaveUnidad")?.Value ?? "");
                                    table.Cell().Border(0.5f).Padding(2).Text(c.Attribute("Unidad")?.Value ?? "");
                                    table.Cell().Border(0.5f).AlignRight().Padding(2).Text(c.Attribute("ValorUnitario")?.Value ?? "");
                                    table.Cell().Border(0.5f).AlignRight().Padding(2).Text(c.Attribute("Importe")?.Value ?? "");
                                    table.Cell().Border(0.5f).Padding(2).Text(c.Attribute("ObjetoImp")?.Value ?? "");

                                    // === Fila Descripción ===
                                    table.Cell().ColumnSpan(4).Background(gray).Border(0.5f).Padding(2).Text("Descripción").Bold();
                                    table.Cell().ColumnSpan(4).Border(0.5f).Padding(2).Text(c.Attribute("Descripcion")?.Value ?? "");

                                    // === Subtabla de Impuestos ===
                                    var impC = c.Element(cfdi + "Impuestos");
                                    if (impC != null)
                                    {
                                        static string MapImp(string v) => v switch { "001" => "ISR", "002" => "IVA", "003" => "IEPS", _ => v ?? "" };
                                        var tras = impC.Element(cfdi + "Traslados")?.Elements(cfdi + "Traslado") ?? Enumerable.Empty<XElement>();
                                        var rets = impC.Element(cfdi + "Retenciones")?.Elements(cfdi + "Retencion") ?? Enumerable.Empty<XElement>();

                                        table.Cell().ColumnSpan(8).Border(0.5f).Padding(5).Column(sub =>
                                        {
                                            sub.Item().Row(r =>
                                            {
                                                r.ConstantItem(80).Text("Impuesto").Bold().FontSize(7);
                                                r.ConstantItem(80).Text("Tipo").Bold().FontSize(7);
                                                r.ConstantItem(90).Text("Base").Bold().FontSize(7);
                                                r.ConstantItem(80).Text("Tipo\nFactor").Bold().FontSize(7);
                                                r.ConstantItem(80).Text("Tasa o\nCuota").Bold().FontSize(7);
                                                r.RelativeItem().AlignRight().Text("Importe").Bold().FontSize(7);
                                            });

                                            foreach (var t in tras)
                                            {
                                                sub.Item().Row(r =>
                                                {
                                                    r.ConstantItem(80).Text(MapImp(t.Attribute("Impuesto")?.Value)).FontSize(7);
                                                    r.ConstantItem(80).Text("Traslado").FontSize(7);
                                                    r.ConstantItem(90).Text(t.Attribute("Base")?.Value ?? "").FontSize(7);
                                                    r.ConstantItem(80).Text("Tasa").FontSize(7);
                                                    r.ConstantItem(80).Text(t.Attribute("TasaOCuota")?.Value ?? "").FontSize(7);
                                                    r.RelativeItem().AlignRight().Text(t.Attribute("Importe")?.Value ?? "").FontSize(7);
                                                });
                                            }

                                            foreach (var rt in rets)
                                            {
                                                sub.Item().Row(r =>
                                                {
                                                    r.ConstantItem(80).Text(MapImp(rt.Attribute("Impuesto")?.Value)).FontSize(7);
                                                    r.ConstantItem(80).Text("Retención").FontSize(7);
                                                    r.ConstantItem(90).Text(rt.Attribute("Base")?.Value ?? "").FontSize(7);
                                                    r.ConstantItem(80).Text("Tasa").FontSize(7);
                                                    r.ConstantItem(80).Text(rt.Attribute("TasaOCuota")?.Value ?? "").FontSize(7);
                                                    r.RelativeItem().AlignRight().Text(rt.Attribute("Importe")?.Value ?? "").FontSize(7);
                                                });
                                            }
                                        });
                                    }

                                    // === Fila inferior ===
                                    table.Cell().ColumnSpan(4).Background(gray).Border(0.5f).Padding(2).Text("Número de pedimento").Bold();
                                    table.Cell().ColumnSpan(4).Background(gray).Border(0.5f).Padding(2).Text("Número de cuenta predial").Bold();
                                }
                            });

                            // === Cálculo de totales ===
                            decimal D(string? s) => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
                            string Money(decimal v) => $"$ {v:0,0.00}";

                            var imp = comp?.Element(cfdi + "Impuestos");
                            decimal totTras = D(imp?.Attribute("TotalImpuestosTrasladados")?.Value);
                            decimal totRet = D(imp?.Attribute("TotalImpuestosRetenidos")?.Value);
                            decimal retIva = 0m, retIsr = 0m;
                            var rets = imp?.Element(cfdi + "Retenciones")?.Elements(cfdi + "Retencion") ?? Enumerable.Empty<XElement>();
                            foreach (var r in rets)
                            {
                                var impCode = (r.Attribute("Impuesto")?.Value ?? "").Trim();
                                var val = D(r.Attribute("Importe")?.Value);
                                if (impCode == "002") retIva += val;
                                if (impCode == "001") retIsr += val;
                            }

                            decimal sub = D(subtotal);
                            decimal tot = D(total);

                            // === TOTALES ===
                            col.Item().PaddingTop(8).Row(row =>
                            {
                                row.RelativeItem(1.2f).Column(l =>
                                {
                                    string monedaTxt = moneda == "MXN" ? "Peso Mexicano" : moneda;
                                    string formaTxt = forma switch
                                    {
                                        "03" => "Transferencia electrónica de fondos (SPEI)",
                                        "01" => "Efectivo",
                                        "02" => "Cheque nominativo",
                                        _ => forma
                                    };
                                    string metodoTxt = metodo == "PUE" ? "Pago en una sola exhibición"
                                                     : metodo == "PPD" ? "Pago en parcialidades o diferido"
                                                     : metodo;

                                    void Par(string label, string value)
                                    {
                                        l.Item().Row(r =>
                                        {
                                            r.ConstantItem(85).Text(label).Bold().FontSize(8);
                                            r.RelativeItem().Text(value).FontSize(8);
                                        });
                                    }

                                    Par("Moneda:", monedaTxt);
                                    Par("Forma de pago:", formaTxt);
                                    Par("Método de pago:", metodoTxt);
                                });

                                row.RelativeItem().Column(r =>
                                {
                                    float labelWidth = 120;
                                    void Line(string label, string val1, string val2, string val3, bool bold = false)
                                    {
                                        r.Item().Row(rr =>
                                        {
                                            var t1 = rr.ConstantItem(labelWidth).Text(label).FontSize(8);
                                            var t2 = rr.ConstantItem(30).Text(val1).FontSize(8);
                                            var t3 = rr.ConstantItem(40).AlignRight().Text(val2).FontSize(8);
                                            var t4 = rr.RelativeItem().AlignRight().Text(val3).FontSize(8);

                                            if (bold)
                                            {
                                                t1.Bold();
                                                t4.Bold();
                                            }
                                        });
                                    }
                                    Line("Subtotal", "", "", Money(sub));
                                    Line("Impuestos trasladados", "IVA", "16.00%", Money(totTras));
                                    Line("Impuestos retenidos", "IVA", "", Money(retIva));
                                    Line("", "ISR", "", Money(retIsr));
                                    r.Item().PaddingTop(4);
                                    Line("Total", "", "", Money(tot), true);
                                });
                            });

                            // === SELLOS Y QR ===
                            col.Item().PaddingTop(10).Row(r =>
                            {
                                r.RelativeItem(3).Column(c =>
                                {
                                    c.Item().Text($"Folio Fiscal (UUID): {uuid}").Bold();
                                    c.Item().Text($"No. Certificado: {noCertificado}");
                                    c.Item().Text($"No. Certificado SAT: {noCertSAT}");
                                    c.Item().Text($"Fecha Timbrado: {fechaTimbrado}");
                                    c.Item().Text("Sello CFDI:").Bold();
                                    c.Item().Text(selloCFDI);
                                    c.Item().Text("Sello SAT:").Bold();
                                    c.Item().Text(selloSAT);
                                });

                                r.ConstantItem(120).Image(qrBytes).FitArea();
                            });

                            col.Item().LineHorizontal(1);
                            col.Item().AlignCenter().Text("Este documento es una representación impresa del CFDI").Italic();
                        });
                    });
                }).GeneratePdf(rutaPdf);

                Console.WriteLine($"✅ PDF generado correctamente con diseño SAT: {rutaPdf}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al generar PDF: {ex.Message}");
            }
        }












        public byte[] GenerarPDFDesdeXML_Bytes(string rutaXml)
        {
            using var ms = new MemoryStream();

            XDocument xml = XDocument.Load(rutaXml);
            XNamespace cfdi = xml.Root!.Name.Namespace;
            XNamespace tfd = "http://www.sat.gob.mx/TimbreFiscalDigital";

            var comp = xml.Element(cfdi + "Comprobante");
            var emisor = comp?.Element(cfdi + "Emisor");
            var receptor = comp?.Element(cfdi + "Receptor");
            var timbre = comp?.Element(cfdi + "Complemento")?.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "TimbreFiscalDigital");
            var conceptos = comp?.Element(cfdi + "Conceptos")?.Elements(cfdi + "Concepto") ?? Enumerable.Empty<XElement>();

            // ==== MISMO CÓDIGO QUE YA TENÍAS ====
            string uuid = timbre?.Attribute("UUID")?.Value ?? "";
            string total = comp?.Attribute("Total")?.Value ?? "0.00";
            string subtotal = comp?.Attribute("SubTotal")?.Value ?? "0.00";
            string fecha = comp?.Attribute("Fecha")?.Value ?? "";
            string metodo = comp?.Attribute("MetodoPago")?.Value ?? "";
            string forma = comp?.Attribute("FormaPago")?.Value ?? "";
            string moneda = comp?.Attribute("Moneda")?.Value ?? "";
            string lugar = comp?.Attribute("LugarExpedicion")?.Value ?? "";
            string tipoComprobante = comp?.Attribute("TipoDeComprobante")?.Value ?? "";
            string exportacion = comp?.Attribute("Exportacion")?.Value ?? "";
            string noCertificado = comp?.Attribute("NoCertificado")?.Value ?? "";
            string rfcEmisor = emisor?.Attribute("Rfc")?.Value ?? "";
            string nombreEmisor = emisor?.Attribute("Nombre")?.Value ?? "";
            string regimenEmisor = emisor?.Attribute("RegimenFiscal")?.Value ?? "";
            string rfcReceptor = receptor?.Attribute("Rfc")?.Value ?? "";
            string nombreReceptor = receptor?.Attribute("Nombre")?.Value ?? "";
            string regimenReceptor = receptor?.Attribute("RegimenFiscalReceptor")?.Value ?? "";
            string usoCfdi = receptor?.Attribute("UsoCFDI")?.Value ?? "";
            string codPostalReceptor = receptor?.Attribute("DomicilioFiscalReceptor")?.Value ?? "";
            string selloCFDI = comp?.Attribute("Sello")?.Value ?? "";
            string selloSAT = timbre?.Attribute("SelloSAT")?.Value ?? "";
            string fechaTimbrado = timbre?.Attribute("FechaTimbrado")?.Value ?? "";
            string noCertSAT = timbre?.Attribute("NoCertificadoSAT")?.Value ?? "";

            string fe = selloCFDI.Length >= 8 ? selloCFDI[^8..] : "";
            string totalQR = decimal.Parse(total, CultureInfo.InvariantCulture).ToString("0.000000", CultureInfo.InvariantCulture);
            string urlQR = $"https://verificacfdi.facturaelectronica.sat.gob.mx/default.aspx?id={uuid}&re={rfcEmisor}&rr={rfcReceptor}&tt={totalQR}&fe={fe}";

            byte[] qrBytes;
            using (var qrGen = new QRCoder.QRCodeGenerator())
            {
                var qrData = qrGen.CreateQrCode(urlQR, QRCoder.QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCoder.PngByteQRCode(qrData);
                qrBytes = qrCode.GetGraphic(4);
            }

            string logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "LogoSAT.png");

            // ✔ AQUÍ VIENE EXACTAMENTE EL BLOQUE QUE DEBES COPIAR
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Arial"));

                    // === ENCABEZADO ===
                    page.Header().PaddingBottom(10).Row(row =>
                    {
                        if (File.Exists(logoPath))
                            row.ConstantItem(120).Height(60).Image(logoPath).FitWidth();
                        else
                            row.ConstantItem(120).Height(60).Text("SAT").Bold().FontSize(14);

                        row.RelativeItem().AlignMiddle().AlignCenter()
                            .Text("COMPROBANTE FISCAL DIGITAL POR INTERNET (CFDI 4.0)")
                            .Bold().FontSize(14);
                    });

                    // === CONTENIDO PRINCIPAL ===
                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        // === BLOQUE DATOS GENERALES ===
                        static string MapRegimen(string c) => c switch
                        {
                            "626" => "Régimen Simplificado de Confianza",
                            "603" => "Personas Morales con Fines no Lucrativos",
                            _ => c ?? "-"
                        };

                        static string MapUsoCfdi(string c) => c switch
                        {
                            "G03" => "Gastos en general",
                            "D01" => "Honorarios médicos, dentales y hospitalarios",
                            "P01" => "Por definir",
                            _ => c ?? "-"
                        };

                        col.Item().PaddingBottom(5).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1.5f);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1.5f);
                            });

                            void AddRow(string leftLabel, string leftValue, string rightLabel, string rightValue)
                            {
                                table.Cell().Text(leftLabel).Bold();
                                table.Cell().Text(leftValue);
                                table.Cell().Text(rightLabel).Bold();
                                table.Cell().Text(rightValue);
                            }

                            AddRow("RFC emisor:", rfcEmisor, "Folio fiscal:", uuid);
                            AddRow("Nombre emisor:", nombreEmisor, "No. de serie del CSD:", noCertificado);
                            AddRow("RFC receptor:", rfcReceptor, "Código postal / Fecha emisión:", $"{lugar} {fecha}");
                            AddRow("Nombre receptor:", nombreReceptor, "Efecto de comprobante:", tipoComprobante == "I" ? "Ingreso" : tipoComprobante);
                            AddRow("Código postal del receptor:", codPostalReceptor, "Régimen fiscal:", MapRegimen(regimenEmisor));
                            AddRow("Régimen fiscal receptor:", MapRegimen(regimenReceptor), "Exportación:", exportacion == "01" ? "No aplica" : exportacion);
                            AddRow("Uso CFDI:", MapUsoCfdi(usoCfdi), "", "");
                        });

                        // === TÍTULO CONCEPTOS ===
                        col.Item().PaddingTop(10).Text("Conceptos").Bold().FontSize(10);

                        // === TABLA DE CONCEPTOS ===
                        col.Item().Table(table =>
                        {
                            string gray = "#E6E6E6";
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(70);
                                cols.ConstantColumn(65);
                                cols.ConstantColumn(55);
                                cols.ConstantColumn(55);
                                cols.ConstantColumn(70);
                                cols.ConstantColumn(70);
                                cols.ConstantColumn(70);
                                cols.ConstantColumn(90);
                            });

                            table.Header(h =>
                            {
                                void HC(string t) => h.Cell().Background(gray).Border(0.5f).Padding(2).Text(t).Bold().FontSize(7);
                                HC("Clave del producto y/o servicio");
                                HC("No. identificación");
                                HC("Cantidad");
                                HC("Clave de unidad");
                                HC("Unidad");
                                HC("Valor unitario");
                                HC("Importe");
                                HC("Objeto impuesto");
                            });

                            foreach (var c in conceptos)
                            {
                                table.Cell().Border(0.5f).Padding(2).Text(c.Attribute("ClaveProdServ")?.Value ?? "");
                                table.Cell().Border(0.5f).Padding(2).Text(c.Attribute("NoIdentificacion")?.Value ?? "");
                                table.Cell().Border(0.5f).Padding(2).Text(c.Attribute("Cantidad")?.Value ?? "");
                                table.Cell().Border(0.5f).Padding(2).Text(c.Attribute("ClaveUnidad")?.Value ?? "");
                                table.Cell().Border(0.5f).Padding(2).Text(c.Attribute("Unidad")?.Value ?? "");
                                table.Cell().Border(0.5f).AlignRight().Padding(2).Text(c.Attribute("ValorUnitario")?.Value ?? "");
                                table.Cell().Border(0.5f).AlignRight().Padding(2).Text(c.Attribute("Importe")?.Value ?? "");
                                table.Cell().Border(0.5f).Padding(2).Text(c.Attribute("ObjetoImp")?.Value ?? "");

                                // === Fila Descripción ===
                                table.Cell().ColumnSpan(4).Background(gray).Border(0.5f).Padding(2).Text("Descripción").Bold();
                                table.Cell().ColumnSpan(4).Border(0.5f).Padding(2).Text(c.Attribute("Descripcion")?.Value ?? "");

                                // === Subtabla de Impuestos ===
                                var impC = c.Element(cfdi + "Impuestos");
                                if (impC != null)
                                {
                                    static string MapImp(string v) => v switch { "001" => "ISR", "002" => "IVA", "003" => "IEPS", _ => v ?? "" };
                                    var tras = impC.Element(cfdi + "Traslados")?.Elements(cfdi + "Traslado") ?? Enumerable.Empty<XElement>();
                                    var rets = impC.Element(cfdi + "Retenciones")?.Elements(cfdi + "Retencion") ?? Enumerable.Empty<XElement>();

                                    table.Cell().ColumnSpan(8).Border(0.5f).Padding(5).Column(sub =>
                                    {
                                        sub.Item().Row(r =>
                                        {
                                            r.ConstantItem(80).Text("Impuesto").Bold().FontSize(7);
                                            r.ConstantItem(80).Text("Tipo").Bold().FontSize(7);
                                            r.ConstantItem(90).Text("Base").Bold().FontSize(7);
                                            r.ConstantItem(80).Text("Tipo\nFactor").Bold().FontSize(7);
                                            r.ConstantItem(80).Text("Tasa o\nCuota").Bold().FontSize(7);
                                            r.RelativeItem().AlignRight().Text("Importe").Bold().FontSize(7);
                                        });

                                        foreach (var t in tras)
                                        {
                                            sub.Item().Row(r =>
                                            {
                                                r.ConstantItem(80).Text(MapImp(t.Attribute("Impuesto")?.Value)).FontSize(7);
                                                r.ConstantItem(80).Text("Traslado").FontSize(7);
                                                r.ConstantItem(90).Text(t.Attribute("Base")?.Value ?? "").FontSize(7);
                                                r.ConstantItem(80).Text("Tasa").FontSize(7);
                                                r.ConstantItem(80).Text(t.Attribute("TasaOCuota")?.Value ?? "").FontSize(7);
                                                r.RelativeItem().AlignRight().Text(t.Attribute("Importe")?.Value ?? "").FontSize(7);
                                            });
                                        }

                                        foreach (var rt in rets)
                                        {
                                            sub.Item().Row(r =>
                                            {
                                                r.ConstantItem(80).Text(MapImp(rt.Attribute("Impuesto")?.Value)).FontSize(7);
                                                r.ConstantItem(80).Text("Retención").FontSize(7);
                                                r.ConstantItem(90).Text(rt.Attribute("Base")?.Value ?? "").FontSize(7);
                                                r.ConstantItem(80).Text("Tasa").FontSize(7);
                                                r.ConstantItem(80).Text(rt.Attribute("TasaOCuota")?.Value ?? "").FontSize(7);
                                                r.RelativeItem().AlignRight().Text(rt.Attribute("Importe")?.Value ?? "").FontSize(7);
                                            });
                                        }
                                    });
                                }

                                // === Fila inferior ===
                                table.Cell().ColumnSpan(4).Background(gray).Border(0.5f).Padding(2).Text("Número de pedimento").Bold();
                                table.Cell().ColumnSpan(4).Background(gray).Border(0.5f).Padding(2).Text("Número de cuenta predial").Bold();
                            }
                        });

                        // === Cálculo de totales ===
                        decimal D(string? s) => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
                        string Money(decimal v) => $"$ {v:0,0.00}";

                        var imp = comp?.Element(cfdi + "Impuestos");
                        decimal totTras = D(imp?.Attribute("TotalImpuestosTrasladados")?.Value);
                        decimal totRet = D(imp?.Attribute("TotalImpuestosRetenidos")?.Value);
                        decimal retIva = 0m, retIsr = 0m;
                        var rets = imp?.Element(cfdi + "Retenciones")?.Elements(cfdi + "Retencion") ?? Enumerable.Empty<XElement>();
                        foreach (var r in rets)
                        {
                            var impCode = (r.Attribute("Impuesto")?.Value ?? "").Trim();
                            var val = D(r.Attribute("Importe")?.Value);
                            if (impCode == "002") retIva += val;
                            if (impCode == "001") retIsr += val;
                        }

                        decimal sub = D(subtotal);
                        decimal tot = D(total);

                        // === TOTALES ===
                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem(1.2f).Column(l =>
                            {
                                string monedaTxt = moneda == "MXN" ? "Peso Mexicano" : moneda;
                                string formaTxt = forma switch
                                {
                                    "03" => "Transferencia electrónica de fondos (SPEI)",
                                    "01" => "Efectivo",
                                    "02" => "Cheque nominativo",
                                    _ => forma
                                };
                                string metodoTxt = metodo == "PUE" ? "Pago en una sola exhibición"
                                                 : metodo == "PPD" ? "Pago en parcialidades o diferido"
                                                 : metodo;

                                void Par(string label, string value)
                                {
                                    l.Item().Row(r =>
                                    {
                                        r.ConstantItem(85).Text(label).Bold().FontSize(8);
                                        r.RelativeItem().Text(value).FontSize(8);
                                    });
                                }

                                Par("Moneda:", monedaTxt);
                                Par("Forma de pago:", formaTxt);
                                Par("Método de pago:", metodoTxt);
                            });

                            row.RelativeItem().Column(r =>
                            {
                                float labelWidth = 120;
                                void Line(string label, string val1, string val2, string val3, bool bold = false)
                                {
                                    r.Item().Row(rr =>
                                    {
                                        var t1 = rr.ConstantItem(labelWidth).Text(label).FontSize(8);
                                        var t2 = rr.ConstantItem(30).Text(val1).FontSize(8);
                                        var t3 = rr.ConstantItem(40).AlignRight().Text(val2).FontSize(8);
                                        var t4 = rr.RelativeItem().AlignRight().Text(val3).FontSize(8);

                                        if (bold)
                                        {
                                            t1.Bold();
                                            t4.Bold();
                                        }
                                    });
                                }
                                Line("Subtotal", "", "", Money(sub));
                                Line("Impuestos trasladados", "IVA", "16.00%", Money(totTras));
                                Line("Impuestos retenidos", "IVA", "", Money(retIva));
                                Line("", "ISR", "", Money(retIsr));
                                r.Item().PaddingTop(4);
                                Line("Total", "", "", Money(tot), true);
                            });
                        });

                        // === SELLOS Y QR ===
                        col.Item().PaddingTop(10).Row(r =>
                        {
                            r.RelativeItem(3).Column(c =>
                            {
                                c.Item().Text($"Folio Fiscal (UUID): {uuid}").Bold();
                                c.Item().Text($"No. Certificado: {noCertificado}");
                                c.Item().Text($"No. Certificado SAT: {noCertSAT}");
                                c.Item().Text($"Fecha Timbrado: {fechaTimbrado}");
                                c.Item().Text("Sello CFDI:").Bold();
                                c.Item().Text(selloCFDI);
                                c.Item().Text("Sello SAT:").Bold();
                                c.Item().Text(selloSAT);
                            });

                            r.ConstantItem(120).Image(qrBytes).FitArea();
                        });

                        col.Item().LineHorizontal(1);
                        col.Item().AlignCenter().Text("Este documento es una representación impresa del CFDI").Italic();
                    });
                });
            }).GeneratePdf(ms);

            return ms.ToArray();
        }









        // =====================================================
        // 🔹 Copiar certificados
        // =====================================================
        private void CopiarCertificadosSiNoExisten(CertificadoSAT certificado, string carpetaRFC)
        {
            try
            {
                string destinoPfx = Path.Combine(carpetaRFC, "certificado.pfx");
                string destinoCer = Path.Combine(carpetaRFC, "certificado.cer");
                string destinoKey = Path.Combine(carpetaRFC, "llave.key");

                if (!File.Exists(destinoPfx) && File.Exists(certificado.archivo_pfx!))
                    File.Copy(certificado.archivo_pfx!, destinoPfx, overwrite: false);
                if (!File.Exists(destinoCer) && File.Exists(certificado.archivo_cer!))
                    File.Copy(certificado.archivo_cer!, destinoCer, overwrite: false);
                if (!File.Exists(destinoKey) && File.Exists(certificado.archivo_key!))
                    File.Copy(certificado.archivo_key!, destinoKey, overwrite: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copiando certificados: {ex.Message}");
            }
        }

        // =====================================================
        // 🔹 DTOs
        // =====================================================
        public class SatSolicitudResultado
        {
            public int EstadoSolicitud { get; set; }
            public string CodigoEstado { get; set; } = "";
            public string Mensaje { get; set; } = "";
            public string Token { get; set; } = "";
        }

        public class SatVerificacionResultado
        {
            public int EstadoSolicitud { get; set; }
            public string CodigoEstado { get; set; } = "";
            public string Mensaje { get; set; } = "";
            public int NumeroCfdis { get; set; }
        }

        public class SatDescargaResultado
        {
            public string ArchivoZip { get; set; } = "";
            public string CarpetaXml { get; set; } = "";
            public string CarpetaPdf { get; set; } = "";
            public string Estado { get; set; } = "";
            public string Mensaje { get; set; } = "";
        }
    }
}