
using API_asemp.Datos;
using API_asemp.Models;
using API_asemp.Models.BD;
using API_asemp.Servicios; // 👈 necesario para usar SatService
using ASEMP.CFDI.DescargaMasiva.Helpers;
using ASEMP.CFDI.DescargaMasiva.Models;
using Herramientas.Validaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;

namespace API_asemp.Controllers
{
    [RequireAccion("sat.ver")]
    [ApiController]
    [Route("solicitudessat")]
    [Authorize]
    public class SolicitudesSATController : ControllerBase
    {
        private readonly SolicitudesSAT _solicitudes;
        private readonly SatService _sat; // 👈 referencia al servicio real del SAT

        public SolicitudesSATController(SolicitudesSAT solicitudes, SatService sat)
        {
            _solicitudes = solicitudes;
            _sat = sat;
        }

        // ===============================================================
        // LISTAR SOLICITUDES
        // ===============================================================
        [RequireAccion("sat.ver")]
        [HttpGet("lista")]
        public IActionResult GetLista()
        {
            try
            {
                var lista = _solicitudes.GetLista();
                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // ===============================================================
        // BUSCAR POR ID
        // ===============================================================
        [RequireAccion("sat.ver_detalle")]

        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _solicitudes.Get(id);
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        // ===============================================================
        // BUSCAR CON FILTROS
        // ===============================================================
        [RequireAccion("sat.ver")]
        [HttpGet("buscar")]
        public IActionResult Buscar([FromQuery] SolicitudSATFiltro filtro)
        {
            try
            {
                var resultado = _solicitudes.Buscar(filtro);
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }



        // ===============================================================
        // CREAR SOLICITUD LOCAL
        // ===============================================================
        // ===============================================================
        // CREAR SOLICITUD Y DESCARGAR AUTOMÁTICAMENTE SI YA ESTÁ LISTA
        // ===============================================================
        [RequireAccion("sat.crear_solicitud")]
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] SolicitudSAT solicitud)
        {
            try
            {
                Ensure.ValidarNulo(solicitud, "El objeto solicitud no puede ser nulo.");

                // 🔹 Usuario autenticado
                var userIdClaim = User.FindFirst("id") ?? User.FindFirst("sub") ?? User.FindFirst("nameid");
                if (userIdClaim == null)
                    return Unauthorized("No se pudo determinar el usuario autenticado.");
                solicitud.usuario_id = int.Parse(userIdClaim.Value);

                // 🔹 Fechas UTC
                solicitud.fecha_inicio = solicitud.fecha_inicio.ToUniversalTime();
                solicitud.fecha_fin = solicitud.fecha_fin.ToUniversalTime();
                solicitud.fecha_creacion = DateTime.UtcNow;

                // 🔹 Certificado activo del cliente
                var certificado = _solicitudes.DbContext.certificados_sat
                    .FirstOrDefault(c => c.cliente_id == solicitud.cliente_id && c.estatus);
                if (certificado == null)
                    return BadRequest("El cliente no tiene un certificado activo.");

                solicitud.certificado_id = certificado.id;
                //solicitud.rfc_emisor = certificado.rfc;
                if (solicitud.tipo_solicitud?.ToLower() == "emitidos")
                {
                    solicitud.rfc_emisor = certificado.rfc;           // el cliente EMITE
                    solicitud.rfc_receptor = "";
                }
                else // recibidos
                {
                    solicitud.rfc_emisor = "";                      // NO SE ENVÍA RFC EMISOR
                    solicitud.rfc_receptor = certificado.rfc;         // el cliente RECIBE
                }


                // 🔹 Guardar en BD antes de enviar al SAT
                var resultadoLocal = _solicitudes.Registrar(solicitud);
                if (!resultadoLocal.IsSuccess)
                    return BadRequest(resultadoLocal.Mensaje);

                // 🔹 Enviar al SAT
                var respuesta = await _sat.EnviarSolicitudAsync(solicitud, certificado);

                solicitud.token = respuesta.Token;
                solicitud.codigo_estado = respuesta.CodigoEstado;
                solicitud.mensaje = respuesta.Mensaje;
                solicitud.estado_solicitud = respuesta.EstadoSolicitud;
                solicitud.fecha_ultima_verificacion = DateTime.UtcNow;

                await _solicitudes.DbContext.SaveChangesAsync();

                // 🔹 Registrar la verificación inicial
                var verif = new VerificacionesSAT
                {
                    SolicitudId = solicitud.id,
                    CodigoEstado = respuesta.CodigoEstado,
                    EstadoSolicitud = (short?)respuesta.EstadoSolicitud,
                    Mensaje = respuesta.Mensaje,
                    NumeroCfdis = 0,
                    FechaVerificacion = DateTime.UtcNow
                };
                _solicitudes.DbContext.verificaciones_sat.Add(verif);
                await _solicitudes.DbContext.SaveChangesAsync();

                // ✅ Si ya está lista, descargar inmediatamente
                if (respuesta.EstadoSolicitud == 3 && respuesta.CodigoEstado == "5000")
                {
                    // Autenticar PFX antes de descargar
                    string basePath = _sat.GetStoragePath();
                    string rutaPfx = Path.Combine(basePath, certificado.archivo_pfx!);
                    byte[] pfxBytes = await System.IO.File.ReadAllBytesAsync(rutaPfx);
                    var cert = X509Certificate2Helper.GetCertificate(pfxBytes, certificado.contrasena!);

                    var authRes = await _sat.AutenticacionAsync(cert);
                    if (!authRes.Ok)
                        return BadRequest("Error autenticando con el SAT.");

                    var accessToken = new ASEMP.CFDI.DescargaMasiva.Models.AccessToken(authRes.Token);



                    var descarga = await _sat.DescargarPaqueteAsync(solicitud.token!, certificado, accessToken, solicitud.tipo_solicitud
);

                    var registroDescarga = new DescargaSAT
                    {
                        solicitud_id = solicitud.id,
                        archivo_zip = descarga.ArchivoZip,
                        carpeta_extraccion = descarga.CarpetaXml,
                        estatus = descarga.Estado,
                        fecha_descarga = DateTime.UtcNow
                    };

                    _solicitudes.DbContext.descargas_sat.Add(registroDescarga);
                    await _solicitudes.DbContext.SaveChangesAsync();

                    return Ok(new
                    {
                        ok = true,
                        mensaje = "Solicitud terminada y CFDIs descargados correctamente.",
                        solicitud.id,
                        solicitud.token,
                        descarga = new
                        {
                            zip = descarga.ArchivoZip,
                            xml = descarga.CarpetaXml,
                            pdf = descarga.CarpetaPdf
                        }
                    });
                }

                // 🔹 Si no está lista todavía
                return Ok(new
                {
                    ok = true,
                    mensaje = "Solicitud registrada correctamente. Aún no hay CFDIs disponibles.",
                    solicitud.id,
                    solicitud.token,
                    solicitud.estado_solicitud
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error creando solicitud: {e}");
                
                return BadRequest(e.ToString());
            }
        }


        // ===============================================================
        // ENVIAR SOLICITUD AL SAT
        // ===============================================================
        [RequireAccion("sat.crear_solicitud")]
        [HttpPost("{id}/enviar-sat")]

        [AllowAnonymous] // ← solo durante pruebas
        public async Task<IActionResult> EnviarSat(int id)
        {
            var solicitud = _solicitudes.Get(id);
            if (solicitud == null)
                return NotFound("No se encontró la solicitud.");

            // Buscar certificado asociado
            var certificado = _solicitudes.DbContext.certificados_sat
                .FirstOrDefault(c => c.id == solicitud.certificado_id);
            if (certificado == null)
                return BadRequest("No se encontró el certificado asociado.");

            // 🔹 Enviar solicitud al SAT
            var respuesta = await _sat.EnviarSolicitudAsync(solicitud, certificado);

            solicitud.token = respuesta.Token;
            solicitud.codigo_estado = respuesta.CodigoEstado;
            solicitud.mensaje = respuesta.Mensaje;
            solicitud.estado_solicitud = respuesta.EstadoSolicitud;
            solicitud.fecha_ultima_verificacion = DateTime.UtcNow;

            _solicitudes.DbContext.SaveChanges();

            return Ok(new
            {
                ok = true,
                solicitud.id,
                solicitud.estado_solicitud,
                solicitud.codigo_estado,
                solicitud.mensaje
            });
        }

        // ===============================================================
        // VERIFICAR SOLICITUD SAT
        // ===============================================================
        // ===============================================================
        // VERIFICAR SOLICITUD SAT Y DESCARGAR SI ESTÁ LISTA
        // ===============================================================
        [RequireAccion("sat.verificar_solicitud")]
        [HttpPost("{id}/verificar")]
        [AllowAnonymous] // ← solo durante pruebas
        public async Task<IActionResult> Verificar(int id)
        {
            try
            {
                // 1️⃣ Obtener solicitud y certificado
                var solicitud = _solicitudes.Get(id);
                if (solicitud == null)
                    return NotFound("No se encontró la solicitud.");

                var certificado = _solicitudes.DbContext.certificados_sat
                    .FirstOrDefault(c => c.id == solicitud.certificado_id);
                if (certificado == null)
                    return BadRequest("No se encontró el certificado asociado.");

                // 2️⃣ Validar archivo PFX
                string basePath = _sat.GetStoragePath();
                var pfxPath = Path.Combine(basePath, certificado.archivo_pfx!);
                if (!System.IO.File.Exists(pfxPath))
                    return BadRequest($"No se encontró el archivo PFX: {pfxPath}");

                byte[] pfxBytes = await System.IO.File.ReadAllBytesAsync(pfxPath);
                var cert = X509Certificate2Helper.GetCertificate(pfxBytes, certificado.contrasena!);

                // 3️⃣ Autenticación SAT para obtener AccessToken temporal
                var authRes = await _sat.AutenticacionAsync(cert);
                if (!authRes.Ok)
                    return BadRequest("Error autenticando con el SAT.");

                var accessToken = new ASEMP.CFDI.DescargaMasiva.Models.AccessToken(authRes.Token);

                // 4️⃣ Verificar estado de solicitud en el SAT
                var resultado = await _sat.VerificarSolicitudAsync(solicitud, certificado);

                solicitud.estado_solicitud = resultado.EstadoSolicitud;
                solicitud.codigo_estado = resultado.CodigoEstado;
                solicitud.mensaje = resultado.Mensaje;
                solicitud.fecha_ultima_verificacion = DateTime.UtcNow;

                var verif = new VerificacionesSAT
                {
                    SolicitudId = solicitud.id,
                    CodigoEstado = resultado.CodigoEstado,
                    EstadoSolicitud = (short?)resultado.EstadoSolicitud,
                    Mensaje = resultado.Mensaje,
                    NumeroCfdis = resultado.NumeroCfdis,
                    FechaVerificacion = DateTime.UtcNow
                };
                _solicitudes.DbContext.verificaciones_sat.Add(verif);
                await _solicitudes.DbContext.SaveChangesAsync();

                // 5️⃣ Si ya está lista para descarga
                if (resultado.EstadoSolicitud == 3 && resultado.CodigoEstado == "5000")
                {
                    string requestId = solicitud.token!;
                    var descarga = await _sat.DescargarPaqueteAsync(requestId, certificado, accessToken, solicitud.tipo_solicitud
);

                    if (descarga.Estado == "Error")
                        return BadRequest(new { ok = false, mensaje = descarga.Mensaje });

                    // Registrar descarga en la BD
                    var registroDescarga = new DescargaSAT
                    {
                        solicitud_id = solicitud.id,
                        archivo_zip = descarga.ArchivoZip,
                        carpeta_extraccion = descarga.CarpetaXml,
                        estatus = descarga.Estado,
                        fecha_descarga = DateTime.UtcNow
                    };
                    _solicitudes.DbContext.descargas_sat.Add(registroDescarga);
                    await _solicitudes.DbContext.SaveChangesAsync();

                    return Ok(new
                    {
                        ok = true,
                        mensaje = "Solicitud verificada y CFDIs descargados automáticamente.",
                        detalles = new
                        {
                            estado = resultado.EstadoSolicitud,
                            codigo = resultado.CodigoEstado,
                            descripcion = resultado.Mensaje,
                            cfdis = resultado.NumeroCfdis,
                            archivoZip = descarga.ArchivoZip,
                            carpetaXml = descarga.CarpetaXml,
                            carpetaPdf = descarga.CarpetaPdf
                        }
                    });
                }

                // 6️⃣ Si aún no está lista
                return Ok(new
                {
                    ok = true,
                    mensaje = "Verificación completada correctamente. CFDIs aún no disponibles.",
                    detalles = new
                    {
                        estado = resultado.EstadoSolicitud,
                        codigo = resultado.CodigoEstado,
                        descripcion = resultado.Mensaje,
                        cfdis = resultado.NumeroCfdis
                    }
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error verificando solicitud: {e}");
                return BadRequest(new { ok = false, mensaje = e.Message });
            }
        }






        // ===============================================================
        // EDITAR SOLICITUD
        // ===============================================================
        [RequireAccion("sat.editar")]
        [HttpPut("editar/{id}")]
        public IActionResult Editar(int id, [FromBody] SolicitudSAT solicitud)
        {
            try
            {
                Ensure.ValidarNulo(solicitud, "El objeto solicitud no puede ser nulo.");
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                solicitud.id = id;

                var resultado = _solicitudes.Actualizar(solicitud);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // ===============================================================
        // ELIMINAR SOLICITUD
        // ===============================================================
        [RequireAccion("sat.eliminar")]
        [HttpDelete("eliminar/{id}")]
        public IActionResult Eliminar(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _solicitudes.Eliminar(id);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }



        
    }
}
