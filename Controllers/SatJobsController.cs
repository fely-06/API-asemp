using API_asemp.Contextos;
using API_asemp.Datos;
using API_asemp.Models.SAT;
using API_asemp.Servicios.SAT;
using Herramientas.Validaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API_asemp.Controllers
{
    [RequireAccion("sat.ver")]                // Permiso base para ver la sección SAT
    [ApiController]
    [Route("api/sat/jobs")]
    [Authorize]
    public class SatJobsController : ControllerBase
    {
        private readonly SatJobService _jobs;
        private readonly ILogger<SatJobsController> _logger;
        private readonly myDbContext _db;

        public SatJobsController(
            SatJobService jobs,
            ILogger<SatJobsController> logger,
            myDbContext db
        )
        {
            _jobs = jobs;
            _logger = logger;
            _db = db;
        }
        // ===================================================================
        // ✔ 1. CREAR JOB AUTOMÁTICO
        // ===================================================================
        [RequireAccion("sat.crear_solicitud")]
        [HttpPost("crear")]
        public async Task<IActionResult> CrearJob([FromBody] CrearSatJobDTO dto)
        {
            try
            {
                Ensure.ValidarNulo(dto, "El objeto DTO no puede ser nulo.");

                if (dto.ClientesIds == null || dto.ClientesIds.Count == 0)
                    return BadRequest("Debe seleccionar al menos un cliente.");


                // ===============================
                // ✔ Obtener usuario autenticado
                // ===============================
                var userIdClaim = User.FindFirst("id") ?? User.FindFirst("sub") ?? User.FindFirst("nameid");
                if (userIdClaim == null)
                    return Unauthorized("No se pudo determinar el usuario autenticado.");

                dto.UsuarioId = int.Parse(userIdClaim.Value);


                // ==========================
                // ✔ RANGOS SIN ZONA HORARIA
                // ==========================
                dto.RangoInicio = DateTime.SpecifyKind(dto.RangoInicio, DateTimeKind.Unspecified);
                dto.RangoFin = DateTime.SpecifyKind(dto.RangoFin, DateTimeKind.Unspecified);

                // =============================================
                // ✔ FECHA PROGRAMADA SÍ DEBE SER UTC
                // =============================================
                dto.FechaProgramada = DateTime.SpecifyKind(dto.FechaProgramada, DateTimeKind.Utc);



                Console.WriteLine($"RANGO INICIO = {dto.RangoInicio} ({dto.RangoInicio.Kind})");
                Console.WriteLine($"RANGO FIN = {dto.RangoFin} ({dto.RangoFin.Kind})");
                Console.WriteLine($"FECHA PROGRAMADA = {dto.FechaProgramada} ({dto.FechaProgramada.Kind})");


                var job = await _jobs.CrearJobAsync(dto);

                var jobDTO = new
                {
                    id = job.Id,
                    tipoSolicitud = job.TipoSolicitud,
                    rangoInicio = job.RangoInicio,
                    rangoFin = job.RangoFin,
                    fechaProgramada = job.FechaProgramada,
                    intervaloVerificacionMin = job.IntervaloVerificacionMin,
                    maxReintentos = job.MaxReintentos,
                    estado = job.Estado
                };

                return Ok(new
                {
                    ok = true,
                    mensaje = "Job creado correctamente.",
                    job = jobDTO
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando job SAT automático");
                return StatusCode(500, ex.ToString());
            }
        }

        //// ===================================================================
        //// ✔ 1. CREAR JOB AUTOMÁTICO
        //// ===================================================================
        //[RequireAccion("sat.crear_solicitud")]
        //[HttpPost("crear")]
        //public async Task<IActionResult> CrearJob([FromBody] CrearSatJobDTO dto)
        //{
        //    try
        //    {
        //        Ensure.ValidarNulo(dto, "El objeto DTO no puede ser nulo.");

        //        if (dto.ClientesIds == null || dto.ClientesIds.Count == 0)
        //            return BadRequest("Debe seleccionar al menos un cliente.");

        //        // ⚠ PostgreSQL obliga a que TODAS las fechas sean UTC
        //        // ⚠ No convertir a UTC → Rompe fechas del frontend
        //        dto.RangoInicio = DateTime.SpecifyKind(dto.RangoInicio, DateTimeKind.Unspecified);
        //        dto.RangoFin = DateTime.SpecifyKind(dto.RangoFin, DateTimeKind.Unspecified);

        //        // Esta sí debe ser UTC porque se usa para comparar horas reales
        //        dto.FechaProgramada = DateTime.SpecifyKind(dto.FechaProgramada, DateTimeKind.Utc);


        //        var job = await _jobs.CrearJobAsync(dto);


        //        // ============================================
        //        //  EVITAR CICLOS: SOLO REGRESAMOS CAMPOS ÚTILES
        //        // ============================================
        //        var jobDTO = new
        //        {
        //            id = job.Id,
        //            tipoSolicitud = job.TipoSolicitud,
        //            rangoInicio = job.RangoInicio,
        //            rangoFin = job.RangoFin,
        //            fechaProgramada = job.FechaProgramada,
        //            intervaloVerificacionMin = job.IntervaloVerificacionMin,
        //            maxReintentos = job.MaxReintentos,
        //            estado = job.Estado
        //        };

        //        return Ok(new
        //        {
        //            ok = true,
        //            mensaje = "Job creado correctamente.",
        //            job = jobDTO
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error creando job SAT automático");
        //        return StatusCode(500, ex.ToString());
        //    }
        //}


        // ===================================================================
        // ✔ 2. LISTAR JOBS PARA FRONTEND (SIN USAR EL ORQUESTADOR)
        // ===================================================================
        // Importante: No usar ObtenerJobsPendientesAEjecutarAsync aquí
        // porque hace comparación UTC vs Local → error 500
        [RequireAccion("sat.ver")]
        [HttpGet]
        public async Task<IActionResult> ListarJobs()
        {
            try
            {
                var jobs = await _db.SatJobs
                    .Include(j => j.Clientes)
                    .Include(j => j.Logs)
                    .OrderByDescending(j => j.Id)
                    .Select(j => new
                    {
                        j.Id,
                        j.TipoSolicitud,
                        j.RangoInicio,
                        j.RangoFin,
                        j.FechaProgramada,
                        j.IntervaloVerificacionMin,
                        j.MaxReintentos,
                        j.Estado,
                        j.MensajeError,
                        j.FechaCreacion,
                        j.FechaUltimaEjecucion,

                        Clientes = j.Clientes.Select(c => new
                        {
                            c.Id,
                            c.ClienteId,
                            c.Estado,
                            c.Intentos,
                            c.MensajeError
                        }),

                        Logs = j.Logs
                            .OrderByDescending(l => l.Fecha)
                            .Take(10)
                            .Select(l => new
                            {
                                l.Id,
                                l.Accion,
                                l.Detalle,
                                l.Fecha
                            })
                    })
                    .ToListAsync();

                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar jobs");
                return StatusCode(500, ex.ToString());
            }
        }

        // ===================================================================
        // ✔ 3. OBTENER SOLO JOBS PENDIENTES → usado por el Background Service
        // ===================================================================
        [RequireAccion("sat.ver")]
        [HttpGet("pendientes")]
        public async Task<IActionResult> JobsPendientes()
        {
            try
            {
                var utcNow = DateTime.UtcNow;
                var jobs = await _jobs.ObtenerJobsPendientesAEjecutarAsync(utcNow);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo jobs pendientes");
                return StatusCode(500, ex.ToString());
            }
        }

        // ===================================================================
        // ✔ 4. OBTENER DETALLE DE UN JOB (CLIENTES ASOCIADOS)
        // ===================================================================
        [RequireAccion("sat.ver_detalle")]
        [HttpGet("{jobId}")]
        public async Task<IActionResult> ObtenerJob(int jobId)
        {
            var clientes = await _jobs.ObtenerClientesJobAsync(jobId);

            if (clientes == null || clientes.Count == 0)
                return NotFound("No existe el job solicitado.");

            return Ok(clientes);
        }

        // ===================================================================
        // ✔ 5. OBTENER LOGS DEL JOB
        // ===================================================================
        [RequireAccion("sat.ver_detalle")]
        [HttpGet("{jobId}/logs")]
        public async Task<IActionResult> ObtenerLogs(int jobId)
        {
            try
            {
                var logs = await _jobs.GetLogs(jobId);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo logs del job");
                return StatusCode(500, "Error obteniendo logs.");
            }
        }

        // ===================================================================
        // ✔ 6. ELIMINAR JOB
        // ===================================================================
        [RequireAccion("sat.eliminar")]
        [HttpDelete("{jobId}")]
        public async Task<IActionResult> EliminarJob(int jobId)
        {
            var job = await _db.SatJobs.FindAsync(jobId);
            if (job == null)
                return NotFound("Job no encontrado.");

            _db.SatJobs.Remove(job);
            await _db.SaveChangesAsync();

            return Ok(new { ok = true, mensaje = "Job eliminado correctamente" });
        }

        // ===================================================================
        // ✔ 7. EDITAR JOB (solo parámetros básicos)
        // ===================================================================
        [RequireAccion("sat.editar")]
        [HttpPut("{jobId}")]
        public async Task<IActionResult> EditarJob(int jobId, [FromBody] CrearSatJobDTO dto)
        {
            try
            {
                var job = await _db.SatJobs.FindAsync(jobId);
                if (job == null)
                    return NotFound("Job no encontrado.");

                job.TipoSolicitud = dto.TipoSolicitud;

                // Siempre UTC
                //job.RangoInicio = DateTime.SpecifyKind(dto.RangoInicio, DateTimeKind.Utc);
                //job.RangoFin = DateTime.SpecifyKind(dto.RangoFin, DateTimeKind.Utc);
                //job.FechaProgramada = DateTime.SpecifyKind(dto.FechaProgramada, DateTimeKind.Utc);
                job.RangoInicio = DateTime.SpecifyKind(dto.RangoInicio, DateTimeKind.Unspecified);
                job.RangoFin = DateTime.SpecifyKind(dto.RangoFin, DateTimeKind.Unspecified);
                job.FechaProgramada = DateTime.SpecifyKind(dto.FechaProgramada, DateTimeKind.Unspecified);


                job.IntervaloVerificacionMin = dto.IntervaloVerificacionMin;
                job.MaxReintentos = dto.MaxReintentos;

                await _db.SaveChangesAsync();

                return Ok(new { ok = true, mensaje = "Job actualizado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando job");
                return StatusCode(500, ex.ToString());
            }
        }
    }
}
