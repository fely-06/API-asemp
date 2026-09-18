using Microsoft.Extensions.Configuration;

namespace API_asemp.Utilerias
{
    public static class ArchivosHelper
    {
        private static string BasePath(IConfiguration cfg)
        {
            string path = cfg["SatStoragePath"] ?? "C:\\API_asemp_data\\SAT";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;

            //var projectPath = AppDomain.CurrentDomain.BaseDirectory;
            //var defaultPath = Path.Combine(projectPath, "uploads", "SAT");
            //Directory.CreateDirectory(defaultPath);
            //return defaultPath;
        }

        public static string CarpetaCliente(IConfiguration cfg, int clienteId, string rfc)
        {
            string folder = Path.Combine(BasePath(cfg), "Clientes", $"{clienteId}_{rfc}");
            Directory.CreateDirectory(folder);
            return folder;
        }

        public static string CarpetaDescargas(IConfiguration cfg, int clienteId, string rfc)
        {
            string folder = Path.Combine(BasePath(cfg), "Clientes", $"{clienteId}_{rfc}",
                                         "Descargas", $"{DateTime.UtcNow:yyyy_MM}");
            Directory.CreateDirectory(folder);
            return folder;
        }

        public static string GetBasePath(IConfiguration cfg) => BasePath(cfg);
    }
}
