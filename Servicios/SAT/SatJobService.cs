using API_asemp.Contextos;
using API_asemp.Models.BD;
using API_asemp.Models.SAT;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Globalization;

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
        public const int IntervaloMinimoEntreClientesMin = 5;

        /// <summary>
        /// Crea los jobs de una solicitud del formulario:
        ///  - un job "Unica" con los clientes marcados para una sola descarga
        ///  - un job "Diaria" con los clientes marcados como diarios
        /// Los clientes de cada job se solicitan en ORDEN ALFABÉTICO, separados
        /// IntervaloEntreClientesMin minutos (mínimo 5). El job diario arranca
        /// después del último cliente del job único para no empalmar solicitudes.
        /// </summary>
        public async Task<List<SatJob>> CrearJobsAsync(CrearSatJobDTO dto)
        {
            var intervalo = Math.Max(IntervaloMinimoEntreClientesMin, dto.IntervaloEntreClientesMin);

            var diarios = (dto.ClientesDiariosIds ?? new List<int>()).Distinct().ToList();
            var unicos = (dto.ClientesIds ?? new List<int>()).Distinct().Except(diarios).ToList();

            var inicioUtc = DateTime.SpecifyKind(dto.FechaProgramada, DateTimeKind.Utc);
            var creados = new List<SatJob>();

            if (unicos.Count > 0)
            {
                creados.Add(await CrearJobInternoAsync(dto, unicos, "Unica", inicioUtc, intervalo));

                // los diarios empiezan 5 min (o el intervalo) después del último cliente único
                inicioUtc = inicioUtc.AddMinutes(unicos.Count * intervalo);
            }

            if (diarios.Count > 0)
                creados.Add(await CrearJobInternoAsync(dto, diarios, "Diaria", inicioUtc, intervalo));

            return creados;
        }

        private async Task<SatJob> CrearJobInternoAsync(
            CrearSatJobDTO dto,
            List<int> clientesIds,
            string recurrencia,
            DateTime fechaProgramadaUtc,
            int intervaloEntreClientesMin)
        {
            var rangoInicio = dto.RangoInicio;
            var rangoFin = dto.RangoFin;

            // Los jobs diarios siempre bajan el día anterior a su ejecución
            if (recurrencia == "Diaria")
                (rangoInicio, rangoFin) = RangoDiaAnterior(fechaProgramadaUtc);

            var job = new SatJob
            {
                TipoSolicitud = dto.TipoSolicitud,

                // ======================================
                // ✔ ESTAS DOS FECHAS NO VAN EN UTC
                // ======================================
                RangoInicio = DateTime.SpecifyKind(rangoInicio, DateTimeKind.Unspecified),
                RangoFin = DateTime.SpecifyKind(rangoFin, DateTimeKind.Unspecified),

                // ======================================
                // ✔ ESTA FECHA SÍ ES UTC
                // ======================================
                FechaProgramada = DateTime.SpecifyKind(fechaProgramadaUtc, DateTimeKind.Utc),

                IntervaloVerificacionMin = dto.IntervaloVerificacionMin,
                MaxReintentos = dto.MaxReintentos,
                Recurrencia = recurrencia,
                IntervaloEntreClientesMin = intervaloEntreClientesMin,
                Estado = "Pendiente",
                FechaCreacion = DateTime.UtcNow,

                // ✔ AQUÍ SE GUARDA
                UsuarioId = dto.UsuarioId
            };

            _db.SatJobs.Add(job);
            await _db.SaveChangesAsync();

            // Orden alfabético por razón social → hora de envío escalonada
            var ordenados = await OrdenarClientesAlfabeticamenteAsync(clientesIds);

            for (int i = 0; i < ordenados.Count; i++)
            {
                _db.SatJobClientes.Add(new SatJobCliente
                {
                    JobId = job.Id,
                    ClienteId = ordenados[i],
                    SolicitudSatId = null,
                    Estado = "Pendiente",
                    FechaEnvioProgramada = job.FechaProgramada.AddMinutes(i * intervaloEntreClientesMin)
                });
            }

            await _db.SaveChangesAsync();

            await AgregarLog(job.Id, null, "CREAR_JOB",
                $"Recurrencia: {recurrencia} | Tipo: {dto.TipoSolicitud} | " +
                $"Separación: {intervaloEntreClientesMin} min | " +
                $"Clientes (orden alfabético): {string.Join(",", ordenados)}");

            return job;
        }

        // ======================================================
        // ORDEN ALFABÉTICO DE CLIENTES (por razón social)
        // ======================================================
        public async Task<List<int>> OrdenarClientesAlfabeticamenteAsync(IEnumerable<int> clientesIds)
        {
            var ids = clientesIds.Distinct().ToList();

            var nombres = await _db.clientes
                .Where(c => ids.Contains(c.id))
                .Select(c => new { c.id, c.razon_social })
                .ToListAsync();

            return nombres
                .OrderBy(c => c.razon_social ?? "", ComparadorAlfabetico())
                .ThenBy(c => c.id)
                .Select(c => c.id)
                .ToList();
        }

        private static StringComparer ComparadorAlfabetico()
        {
            try
            {
                // Ignora mayúsculas y acentos al ordenar (Á = A, Ñ después de N)
                return StringComparer.Create(CultureInfo.GetCultureInfo("es-MX"), ignoreCase: true);
            }
            catch (CultureNotFoundException)
            {
                // Servidores con InvariantGlobalization
                return StringComparer.InvariantCultureIgnoreCase;
            }
        }

        // ======================================================
        // RANGO DEL DÍA ANTERIOR (para jobs diarios)
        // Usa la hora local del servidor, igual que el resto del módulo
        // (RangoInicio/RangoFin se guardan sin zona y se convierten con ToUniversalTime()).
        // ======================================================
        private static (DateTime inicio, DateTime fin) RangoDiaAnterior(DateTime fechaProgramadaUtc)
        {
            var dia = DateTime.SpecifyKind(fechaProgramadaUtc, DateTimeKind.Utc)
                .ToLocalTime().Date.AddDays(-1);

            var inicio = DateTime.SpecifyKind(dia, DateTimeKind.Unspecified);
            var fin = DateTime.SpecifyKind(dia.AddDays(1).AddSeconds(-1), DateTimeKind.Unspecified);

            return (inicio, fin);
        }

        // ======================================================
        // REPROGRAMAR JOB DIARIO PARA EL DÍA SIGUIENTE
        // Se llama cuando todos los clientes del job ya terminaron.
        // ======================================================
        public async Task ReprogramarJobDiarioAsync(int jobId)
        {
            var job = await _db.SatJobs
                .Include(j => j.Clientes)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null) return;

            var ahora = DateTime.UtcNow;

            // Misma hora del día siguiente; si por algún motivo ya pasó, se salta al próximo día libre
            var siguiente = DateTime.SpecifyKind(job.FechaProgramada, DateTimeKind.Utc).AddDays(1);
            while (siguiente <= ahora)
                siguiente = siguiente.AddDays(1);

            var (inicio, fin) = RangoDiaAnterior(siguiente);

            job.FechaProgramada = siguiente;
            job.RangoInicio = inicio;
            job.RangoFin = fin;
            job.Estado = "Pendiente";
            job.MensajeError = null;

            var ordenados = await OrdenarClientesAlfabeticamenteAsync(job.Clientes.Select(c => c.ClienteId));

            for (int i = 0; i < ordenados.Count; i++)
            {
                var jc = job.Clientes.First(c => c.ClienteId == ordenados[i]);

                jc.Estado = "Pendiente";
                jc.SolicitudSatId = null;
                jc.Intentos = 0;
                jc.MensajeError = null;
                jc.FechaEnvioProgramada = siguiente.AddMinutes(i * job.IntervaloEntreClientesMin);
            }

            await _db.SaveChangesAsync();

            await AgregarLog(job.Id, null, "REPROGRAMADO_DIARIO",
                $"Próxima ejecución: {siguiente:u} | Se descargará el día {inicio:yyyy-MM-dd}");
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
                            ||
                            // Envío escalonado: hay algún cliente cuya hora de solicitud ya llegó
                            j.Clientes.Any(c =>
                                c.SolicitudSatId == null &&
                                c.Estado == "Pendiente" &&
                                (c.FechaEnvioProgramada == null || c.FechaEnvioProgramada <= ahora))
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
