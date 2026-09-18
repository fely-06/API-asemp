using API_asemp.Contextos;
using API_asemp.Models.BD;
using API_asemp.Models.SAT;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace API_asemp.Servicios.SAT
{
    public class SatJobService
    {
        private readonly myDbContext _db;
        private readonly string _satBasePath;

        public SatJobService(myDbContext db, IConfiguration config)
        {
            //_db = db;

            // Leer la ruta configurada en appsettings.json
            //_satBasePath = config["SatStoragePath"] ?? "C:\\API_asemp_data\\SAT";

            _db = db;

            var projectPath = AppDomain.CurrentDomain.BaseDirectory;
            var defaultPath = Path.Combine(projectPath, "uploads", "SAT");
            Directory.CreateDirectory(defaultPath);

            _satBasePath = defaultPath;
        }

        // ======================================================
        // CREAR JOB
        // ======================================================
        public async Task<SatJob> CrearJobAsync(CrearSatJobDTO dto)
        {
            var job = new SatJob
            {
                TipoSolicitud = dto.TipoSolicitud,

                // ======================================
                // ✔ ESTAS DOS FECHAS NO VAN EN UTC
                // ======================================
                RangoInicio = DateTime.SpecifyKind(dto.RangoInicio, DateTimeKind.Unspecified),
                RangoFin = DateTime.SpecifyKind(dto.RangoFin, DateTimeKind.Unspecified),

                // ======================================
                // ✔ ESTA FECHA SÍ ES UTC
                // ======================================
                FechaProgramada = DateTime.SpecifyKind(dto.FechaProgramada, DateTimeKind.Utc),

                IntervaloVerificacionMin = dto.IntervaloVerificacionMin,
                MaxReintentos = dto.MaxReintentos,
                Estado = "Pendiente",
                FechaCreacion = DateTime.UtcNow,


                // ✔ AQUÍ SE GUARDA
        UsuarioId = dto.UsuarioId
            };

            _db.SatJobs.Add(job);
            await _db.SaveChangesAsync();

            foreach (var clienteId in dto.ClientesIds.Distinct())
            {
                var jc = new SatJobCliente
                {
                    JobId = job.Id,
                    ClienteId = clienteId,
                    SolicitudSatId = null,
                    Estado = "Pendiente"
                };

                _db.SatJobClientes.Add(jc);
            }

            await _db.SaveChangesAsync();

            await AgregarLog(job.Id, null, "CREAR_JOB",
                $"Clientes: {string.Join(",", dto.ClientesIds)} | Tipo: {dto.TipoSolicitud}");

            return job;
        }


        // ======================================================
        // OBTENER JOBS PENDIENTES
        // ======================================================
        public async Task<List<SatJob>> ObtenerJobsPendientesAEjecutarAsync(DateTime ahora)
        {
            return await _db.SatJobs
                .Where(j =>
                    (j.Estado == "Pendiente" && j.FechaProgramada <= ahora)
                    ||
                    (
                        (j.Estado == "Solicitando"
                        || j.Estado == "Verificando"
                        || j.Estado == "Descargando")
                        &&
                        (
                            j.FechaUltimaEjecucion == null ||
                            j.FechaUltimaEjecucion.Value.AddMinutes(j.IntervaloVerificacionMin) <= ahora
                        )
                    )
                )
                .Include(j => j.Clientes)
                .ToListAsync();
        }

        // ======================================================
        // MARCAR FECHA DE EJECUCIÓN
        // ======================================================
        public async Task MarcarEjecucionAsync(int jobId)
        {
            var job = await _db.SatJobs.FindAsync(jobId);
            if (job == null) return;

            job.FechaUltimaEjecucion = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // ======================================================
        // CAMBIAR ESTADO DEL JOB
        // ======================================================
        public async Task CambiarEstadoJobAsync(int jobId, string nuevoEstado, string? mensaje = null)
        {
            var job = await _db.SatJobs.FindAsync(jobId);
            if (job == null) return;

            job.Estado = nuevoEstado;

            if (!string.IsNullOrWhiteSpace(mensaje))
                job.MensajeError = mensaje;

            job.FechaUltimaEjecucion = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // ======================================================
        // OBTENER CLIENTES DEL JOB
        // ======================================================
        public async Task<List<SatJobCliente>> ObtenerClientesJobAsync(int jobId)
        {
            return await _db.SatJobClientes
                .Where(c => c.JobId == jobId)
                .Include(c => c.Cliente)
                .Include(c => c.SolicitudSAT)
                .ToListAsync();
        }

        // ======================================================
        // ACTUALIZAR CLIENTE DEL JOB
        // ======================================================
        public async Task ActualizarClienteAsync(SatJobCliente cliente)
        {
            _db.SatJobClientes.Update(cliente);
            await _db.SaveChangesAsync();
        }

        // ======================================================
        // AGREGAR LOG (BD + ARCHIVO NUEVO SIEMPRE)
        // ======================================================
        public async Task AgregarLog(int jobId, int? clienteId, string accion, string? detalle)
        {
            // 1) Guardar en BD
            var log = new SatJobLog
            {
                JobId = jobId,
                ClienteId = clienteId,
                Accion = accion,
                Detalle = detalle,
                Fecha = DateTime.UtcNow
            };

            _db.SatJobLogs.Add(log);
            await _db.SaveChangesAsync();

            // 2) Guardar archivo INDIVIDUAL por log
            try
            {
                string logsPath = Path.Combine(_satBasePath, "logs_jobs");
                Directory.CreateDirectory(logsPath);

                string fileName = $"job_{jobId}_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.log";
                string fullPath = Path.Combine(logsPath, fileName);

                // ✔ SI clienteId ES NULL Y DETALLE CONTIENE LISTA → SE MUESTRA COMPLETA
                string clienteTexto = clienteId?.ToString() ?? "MULTIPLE";

                string linea =
                    $"{DateTime.UtcNow:u} | JOB={jobId} | CLIENTE={clienteTexto} | {accion} | {detalle}\n";

                await File.WriteAllTextAsync(fullPath, linea);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error escribiendo archivo log SAT: " + ex.Message);
            }
        }


        public async Task<List<SatJobLog>> GetLogs(int jobId)
        {
            return await _db.SatJobLogs
                .Where(l => l.JobId == jobId)
                .OrderByDescending(l => l.Fecha)
                .ToListAsync();
        }
    }
}
