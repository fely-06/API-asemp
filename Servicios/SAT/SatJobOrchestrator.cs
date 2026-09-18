using API_asemp.Contextos;
using API_asemp.Models.BD;
using ASEMP.CFDI.DescargaMasiva.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace API_asemp.Servicios.SAT
{
    public class SatJobOrchestrator
    {
        private readonly SatJobService _jobs;
        private readonly SatService _sat;
        private readonly myDbContext _db;

        public SatJobOrchestrator(SatJobService jobs, SatService sat, myDbContext db)
        {
            _jobs = jobs;
            _sat = sat;
            _db = db;
        }

        public async Task ProcesarJobsAsync(CancellationToken token)
        {
            var ahora = DateTime.UtcNow;


            var jobs = await _jobs.ObtenerJobsPendientesAEjecutarAsync(ahora);

            foreach (var job in jobs)
            {
                if (token.IsCancellationRequested)
                    break;

                try
                {
                    switch (job.Estado)
                    {
                        case "Pendiente":
                            await _jobs.CambiarEstadoJobAsync(job.Id, "Solicitando");
                            await EnviarSolicitudes(job, token);
                            break;

                        case "Solicitando":
                        case "Verificando":
                        case "Descargando":
                            await VerificarYDescargar(job, token);
                            break;
                    }

                    // Muy importante: registrar que este job se procesó en este ciclo
                    await _jobs.MarcarEjecucionAsync(job.Id);
                }
                catch (Exception ex)
                {
                    // Si algo truena a nivel job, lo registramos como error en el job
                    await _jobs.CambiarEstadoJobAsync(job.Id, "Error", ex.Message);
                    await _jobs.AgregarLog(job.Id, null, "JOB_ERROR", ex.Message);
                }
            }
        }

        // =========================================================
        // 1) ENVIAR SOLICITUDES
        // =========================================================
        private async Task EnviarSolicitudes(SatJob job, CancellationToken token)
        {
            var clientes = await _jobs.ObtenerClientesJobAsync(job.Id);

            foreach (var jc in clientes)
            {
                if (token.IsCancellationRequested)
                    break;

                if (jc.SolicitudSatId != null)
                    continue;

                try
                {
                    var cert = await _db.certificados_sat
                        .FirstOrDefaultAsync(c => c.cliente_id == jc.ClienteId, token);

                    if (cert == null)
                    {
                        jc.Estado = "Error";
                        jc.MensajeError = "Cliente no tiene certificado SAT registrado.";
                        await _jobs.ActualizarClienteAsync(jc);
                        continue;
                    }

                    // ===============================
                    // CONVERTIR A UTC (igual que en Registrar)
                    // ===============================
                    var fechaInicioUtc = job.RangoInicio.ToUniversalTime();
                    var fechaFinUtc = job.RangoFin.ToUniversalTime();

                    // ===============================
                    // RFCs (idéntico a Registrar)
                    // ===============================
                    string tipo = job.TipoSolicitud.ToLowerInvariant();
                    string rfcEmisor;
                    string rfcReceptor;

                    if (tipo == "emitidos")
                    {
                        rfcEmisor = cert.rfc;
                        rfcReceptor = "";
                    }
                    else // "recibidos"
                    {
                        rfcEmisor = "";
                        rfcReceptor = cert.rfc;
                    }

                    // ===============================
                    // CREAR SOLICITUD
                    // ===============================
                    var solicitud = new SolicitudSAT
                    {
                        usuario_id = job.UsuarioId,                        // si no tienes este campo usa 2 temporalmente
                        cliente_id = jc.ClienteId,
                        certificado_id = cert.id,

                        tipo_solicitud = tipo,
                        fecha_inicio = fechaInicioUtc,
                        fecha_fin = fechaFinUtc,

                        rfc_emisor = rfcEmisor,
                        rfc_receptor = rfcReceptor,

                        fecha_creacion = DateTime.UtcNow,
                        estado_solicitud = 0
                    };

                    _db.solicitudes_sat.Add(solicitud);
                    await _db.SaveChangesAsync(token);

                    // ===============================
                    // ENVIAR AL SAT
                    // ===============================
                    var envio = await _sat.EnviarSolicitudAsync(solicitud, cert);

                    solicitud.codigo_estado = envio.CodigoEstado;
                    solicitud.mensaje = envio.Mensaje;
                    solicitud.token = envio.Token;
                    solicitud.estado_solicitud = envio.EstadoSolicitud;

                    if (envio.EstadoSolicitud == 0)
                    {
                        jc.Estado = "Error";
                        jc.MensajeError = envio.Mensaje;
                    }
                    else
                    {
                        jc.SolicitudSatId = solicitud.id;
                        jc.Estado = "Solicitado";
                    }

                    await _db.SaveChangesAsync(token);
                    await _jobs.ActualizarClienteAsync(jc);

                    await _jobs.AgregarLog(job.Id, jc.ClienteId, "SOLICITUD_SAT",
                        $"Solicitud enviada. Estado={envio.EstadoSolicitud}, Token={envio.Token}");
                }
                catch (Exception ex)
                {
                    jc.Intentos++;
                    jc.MensajeError = ex.Message;

                    if (jc.Intentos >= job.MaxReintentos)
                        jc.Estado = "Error";

                    await _jobs.ActualizarClienteAsync(jc);
                    await _jobs.AgregarLog(job.Id, jc.ClienteId, "ERROR_SOLICITUD", ex.Message);
                }
            }

            await _jobs.CambiarEstadoJobAsync(job.Id, "Verificando");
        }


        //// =========================================================
        //// 1) ENVIAR SOLICITUDES
        //// =========================================================
        //private async Task EnviarSolicitudes(SatJob job, CancellationToken token)
        //{
        //    var clientes = await _jobs.ObtenerClientesJobAsync(job.Id);

        //    foreach (var jc in clientes)
        //    {
        //        if (token.IsCancellationRequested)
        //            break;

        //        if (jc.SolicitudSatId != null)
        //            continue;

        //        try
        //        {
        //            var cert = await _db.certificados_sat
        //                .FirstOrDefaultAsync(c => c.cliente_id == jc.ClienteId, token);

        //            if (cert == null)
        //            {
        //                jc.Estado = "Error";
        //                jc.MensajeError = "Cliente no tiene certificado SAT registrado.";
        //                await _jobs.ActualizarClienteAsync(jc);
        //                continue;
        //            }

        //            var solicitud = new SolicitudSAT
        //            {
        //                usuario_id = 2,
        //                cliente_id = jc.ClienteId,
        //                certificado_id = cert.id,

        //                tipo_solicitud = job.TipoSolicitud.ToLower(),
        //                fecha_inicio = DateTime.SpecifyKind(job.RangoInicio, DateTimeKind.Utc),
        //                fecha_fin = DateTime.SpecifyKind(job.RangoFin, DateTimeKind.Utc),
        //                rfc_emisor = cert.rfc, // ← ← ← AGREGAR ESTO

        //                fecha_creacion = DateTime.UtcNow,
        //                estado_solicitud = 0
        //            };


        //            _db.solicitudes_sat.Add(solicitud);
        //            await _db.SaveChangesAsync(token);

        //            var envio = await _sat.EnviarSolicitudAsync(solicitud, cert);

        //            solicitud.codigo_estado = envio.CodigoEstado;
        //            solicitud.mensaje = envio.Mensaje;
        //            solicitud.token = envio.Token;

        //            if (envio.EstadoSolicitud == 0)
        //            {
        //                jc.Estado = "Error";
        //                jc.MensajeError = envio.Mensaje;
        //            }
        //            else
        //            {
        //                jc.SolicitudSatId = solicitud.id;
        //                jc.Estado = "Solicitado";
        //            }

        //            await _db.SaveChangesAsync(token);
        //            await _jobs.ActualizarClienteAsync(jc);

        //            await _jobs.AgregarLog(job.Id, jc.ClienteId, "SOLICITUD_SAT",
        //                $"Solicitud enviada. Estado={envio.EstadoSolicitud}, Token={envio.Token}");
        //        }
        //        catch (Exception ex)
        //        {
        //            jc.Intentos++;
        //            jc.MensajeError = ex.Message;

        //            if (jc.Intentos >= job.MaxReintentos)
        //                jc.Estado = "Error";

        //            await _jobs.ActualizarClienteAsync(jc);
        //            await _jobs.AgregarLog(job.Id, jc.ClienteId, "ERROR_SOLICITUD", ex.Message);
        //        }
        //    }

        //    await _jobs.CambiarEstadoJobAsync(job.Id, "Verificando");
        //}

        private async Task VerificarYDescargar(SatJob job, CancellationToken token)
        {
            var clientes = await _jobs.ObtenerClientesJobAsync(job.Id);

            foreach (var jc in clientes)
            {
                if (token.IsCancellationRequested)
                    break;

                if (jc.SolicitudSatId == null)
                    continue;

                if (jc.Estado is "Terminado" or "SinCFDI" or "Error")
                    continue;

                var sol = await _db.solicitudes_sat
                    .Include(s => s.certificado)
                    .FirstAsync(s => s.id == jc.SolicitudSatId, token);

                var cert = sol.certificado!;   // ← CertificadoSAT (modelo BD)

                try
                {
                    // =========================================================
                    // 1) VERIFICAR SOLICITUD EN EL SAT
                    // =========================================================
                    var ver = await _sat.VerificarSolicitudAsync(sol, cert);

                    sol.codigo_estado = ver.CodigoEstado;
                    sol.mensaje = ver.Mensaje;
                    sol.fecha_ultima_verificacion = DateTime.UtcNow;
                    sol.estado_solicitud = ver.EstadoSolicitud;

                    await _db.SaveChangesAsync(token);

                    // =========================================================
                    // 2) ESTADO 3 = LISTA PARA DESCARGA
                    // =========================================================
                    if (ver.EstadoSolicitud == 3)
                    {
                        if (ver.NumeroCfdis == 0)
                        {
                            jc.Estado = "SinCFDI";
                            await _jobs.ActualizarClienteAsync(jc);

                            await _jobs.AgregarLog(job.Id, jc.ClienteId, "SIN_CFDI",
                                "No se encontraron CFDIs.");

                            continue;
                        }

                        // =========================================================
                        // 3) CARGAR CERTIFICADO PFX → X509Certificate2
                        // =========================================================
                        string basePath = _sat.GetStoragePath();
                        string pfxPath = Path.Combine(basePath, cert.archivo_pfx!);

                        if (!File.Exists(pfxPath))
                        {
                            jc.Estado = "Error";
                            jc.MensajeError = $"PFX no encontrado en {pfxPath}";
                            await _jobs.ActualizarClienteAsync(jc);

                            await _jobs.AgregarLog(job.Id, jc.ClienteId, "ERROR_PFX",
                                $"No se encontró el archivo PFX: {pfxPath}");
                            continue;
                        }

                        byte[] pfxBytes = await File.ReadAllBytesAsync(pfxPath, token);

                        var x509 = new X509Certificate2(
                            pfxBytes,
                            cert.contrasena,
                            X509KeyStorageFlags.MachineKeySet |
                            X509KeyStorageFlags.PersistKeySet |
                            X509KeyStorageFlags.Exportable
                        );

                        // =========================================================
                        // 4) AUTENTICARSE CON EL SAT (tuple: Ok, Token)
                        // =========================================================
                        var auth = await _sat.AutenticacionAsync(x509);   // (bool Ok, string Token)

                        if (!auth.Ok)
                        {
                            jc.Estado = "Error";
                            jc.MensajeError = "Error autenticando con el SAT.";
                            await _jobs.ActualizarClienteAsync(jc);

                            await _jobs.AgregarLog(job.Id, jc.ClienteId, "ERROR_AUTH_SAT",
                                "No se pudo obtener token de autenticación.");
                            continue;
                        }

                        var accessToken = new AccessToken(auth.Token);

                        // =========================================================
                        // 5) DESCARGAR CFDIs (usa CertificadoSAT, NO X509)
                        // =========================================================
                        var descarga = await _sat.DescargarPaqueteAsync(
                            sol.token!,          // requestId (token de solicitud)
                            cert,                // CertificadoSAT (modelo BD)
                            accessToken,         // token nuevo de autenticación
                            sol.tipo_solicitud!  // "emitidos"/"recibidos"
                        );

                        if (descarga.Estado == "Error")
                        {
                            jc.Estado = "Error";
                            jc.MensajeError = descarga.Mensaje;

                            await _jobs.ActualizarClienteAsync(jc);
                            await _jobs.AgregarLog(job.Id, jc.ClienteId, "DESCARGA_ERROR", descarga.Mensaje);
                            continue;
                        }

                        // =========================================================
                        // 6) REGISTRAR DESCARGA EN BD
                        // =========================================================
                        jc.Estado = "Terminado";
                        await _jobs.ActualizarClienteAsync(jc);

                        await _jobs.AgregarLog(job.Id, jc.ClienteId, "DESCARGA_OK",
                            $"CFDIs descargados. ZIP={descarga.ArchivoZip}");

                        var registroDescarga = new DescargaSAT
                        {
                            solicitud_id = sol.id,
                            archivo_zip = descarga.ArchivoZip,
                            carpeta_extraccion = descarga.CarpetaXml,
                            estatus = descarga.Estado,
                            fecha_descarga = DateTime.UtcNow
                        };

                        _db.descargas_sat.Add(registroDescarga);
                        await _db.SaveChangesAsync(token);
                    }
                    else if (ver.EstadoSolicitud == 4)
                    {
                        // ERROR EN SOLICITUD
                        jc.Intentos++;
                        jc.MensajeError = ver.Mensaje;

                        if (jc.Intentos >= job.MaxReintentos)
                            jc.Estado = "Error";

                        await _jobs.ActualizarClienteAsync(jc);
                        await _jobs.AgregarLog(job.Id, jc.ClienteId, "ERROR_VERIFICACION", ver.Mensaje);
                    }
                    else
                    {
                        // SIGUE EN PROCESO
                        jc.Estado = "EnProceso";
                        await _jobs.ActualizarClienteAsync(jc);
                    }
                }
                catch (Exception ex)
                {
                    jc.Intentos++;
                    jc.MensajeError = ex.Message;

                    if (jc.Intentos >= job.MaxReintentos)
                        jc.Estado = "Error";

                    await _jobs.ActualizarClienteAsync(jc);
                    await _jobs.AgregarLog(job.Id, jc.ClienteId, "ERROR_EX", ex.Message);
                }
            }

            // =========================================================
            // 7) SI TODOS TERMINARON → MARCAR JOB COMO FINALIZADO
            // =========================================================
            if (clientes.All(c => c.Estado is "Terminado" or "SinCFDI" or "Error"))
            {
                await _jobs.CambiarEstadoJobAsync(job.Id, "Terminado");
                await _jobs.AgregarLog(job.Id, null, "JOB_FINALIZADO",
                    "Todos los clientes finalizaron.");
            }
        }



        //// =========================================================
        //// 2) VERIFICAR Y DESCARGAR
        //// =========================================================
        //private async Task VerificarYDescargar(SatJob job, CancellationToken token)
        //{
        //    var clientes = await _jobs.ObtenerClientesJobAsync(job.Id);

        //    foreach (var jc in clientes)
        //    {
        //        if (token.IsCancellationRequested)
        //            break;

        //        if (jc.SolicitudSatId == null)
        //            continue;

        //        if (jc.Estado is "Terminado" or "SinCFDI" or "Error")
        //            continue;

        //        var sol = await _db.solicitudes_sat
        //            .Include(s => s.certificado)
        //            .FirstAsync(s => s.id == jc.SolicitudSatId, token);

        //        var cert = sol.certificado!;

        //        try
        //        {
        //            var ver = await _sat.VerificarSolicitudAsync(sol, cert);

        //            sol.codigo_estado = ver.CodigoEstado;
        //            sol.mensaje = ver.Mensaje;
        //            sol.fecha_ultima_verificacion = DateTime.UtcNow;
        //            sol.estado_solicitud = ver.EstadoSolicitud;

        //            await _db.SaveChangesAsync(token);

        //            if (ver.EstadoSolicitud == 3)
        //            {
        //                if (ver.NumeroCfdis == 0)
        //                {
        //                    jc.Estado = "SinCFDI";
        //                    await _jobs.ActualizarClienteAsync(jc);

        //                    await _jobs.AgregarLog(job.Id, jc.ClienteId, "SIN_CFDI",
        //                        "No se encontraron CFDIs.");
        //                }
        //                else
        //                {
        //                    var tokenSAT = new AccessToken(sol.token!);

        //                    var descarga = await _sat.DescargarPaqueteAsync(
        //                        sol.token!,
        //                        cert,
        //                        tokenSAT,
        //                        sol.tipo_solicitud!);

        //                    jc.Estado = "Terminado";
        //                    await _jobs.ActualizarClienteAsync(jc);

        //                    await _jobs.AgregarLog(job.Id, jc.ClienteId, "DESCARGA_OK",
        //                        $"CFDIs descargados. {descarga.Mensaje}");

        //                    var registroDescarga = new DescargaSAT
        //                    {
        //                        solicitud_id = sol.id,
        //                        archivo_zip = descarga.ArchivoZip,
        //                        carpeta_extraccion = descarga.CarpetaXml,
        //                        estatus = descarga.Estado,
        //                        fecha_descarga = DateTime.UtcNow
        //                    };

        //                    _db.descargas_sat.Add(registroDescarga);
        //                    await _db.SaveChangesAsync(token);

        //                }
        //            }
        //            else if (ver.EstadoSolicitud == 4)
        //            {
        //                jc.Intentos++;
        //                jc.MensajeError = ver.Mensaje;

        //                if (jc.Intentos >= job.MaxReintentos)
        //                    jc.Estado = "Error";

        //                await _jobs.ActualizarClienteAsync(jc);
        //                await _jobs.AgregarLog(job.Id, jc.ClienteId, "ERROR_VERIFICACION",
        //                    ver.Mensaje);
        //            }
        //            else
        //            {
        //                jc.Estado = "EnProceso";
        //                await _jobs.ActualizarClienteAsync(jc);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            jc.Intentos++;
        //            jc.MensajeError = ex.Message;

        //            if (jc.Intentos >= job.MaxReintentos)
        //                jc.Estado = "Error";

        //            await _jobs.ActualizarClienteAsync(jc);
        //            await _jobs.AgregarLog(job.Id, jc.ClienteId, "ERROR_EX", ex.Message);
        //        }
        //    }

        //    if (clientes.All(c => c.Estado is "Terminado" or "SinCFDI" or "Error"))
        //    {
        //        await _jobs.CambiarEstadoJobAsync(job.Id, "Terminado");
        //        await _jobs.AgregarLog(job.Id, null, "JOB_FINALIZADO",
        //            "Todos los clientes finalizaron.");
        //    }
        //}
    }
}
