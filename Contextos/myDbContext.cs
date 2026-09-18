    using API_asemp.Controllers;
using API_asemp.Models.BD;
using Microsoft.EntityFrameworkCore;

namespace API_asemp.Contextos
{
    public class myDbContext : DbContext
    {
        public myDbContext(DbContextOptions<myDbContext> options) : base(options) { }
        public DbSet<Departamento> departamentos { get; set; }
        public DbSet<Rol> roles { get; set; }


        //Nuevo
        public DbSet<Usuario> usuarios { get; set; }
        public DbSet<Cliente> clientes { get; set; }
        public DbSet<Cobro_Cliente> cobros_clientes { get; set; }
        public DbSet<CertificadoSAT> certificados_sat { get; set; }
        public DbSet<SolicitudSAT> solicitudes_sat { get; set; }
        public DbSet<DescargaSAT> descargas_sat { get; set; }
        public DbSet<Accion> acciones { get; set; }
        public DbSet<Rol_Accion> roles_acciones { get; set; }
        public DbSet<Catalogo> catalogos { get; set; }

        public DbSet<VerificacionesSAT> verificaciones_sat { get; set; }


        //nuevo para descarga automatica
        public DbSet<SatJob> SatJobs { get; set; }
        public DbSet<SatJobCliente> SatJobClientes { get; set; }
        public DbSet<SatJobLog> SatJobLogs { get; set; }



        // 🔥🔥🔥 AQUI AGREGAS ESTA FUNCIÓN 🔥🔥🔥
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<SatJob>(entity =>
            {
                // Estos dos NO deben ser UTC
                entity.Property(j => j.RangoInicio)
                      .HasColumnType("timestamp without time zone");

                entity.Property(j => j.RangoFin)
                      .HasColumnType("timestamp without time zone");

                // La fecha programada sí es UTC
                entity.Property(j => j.FechaProgramada)
                      .HasColumnType("timestamp with time zone");
            });
        }



    }
}
