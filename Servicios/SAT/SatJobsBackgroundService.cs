using Microsoft.Extensions.Hosting;
using Serilog;

namespace API_asemp.Servicios.SAT
{
    public class SatJobsBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public SatJobsBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("SatJobsBackgroundService iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var orchestrator = scope.ServiceProvider.GetRequiredService<SatJobOrchestrator>();

                    await orchestrator.ProcesarJobsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error ejecutando trabajo automático SAT");
                }

                // Espera 5 minutos entre revisiones
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            Log.Information("SatJobsBackgroundService detenido.");
        }
    }
}
