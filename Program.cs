// ======================================================
// ================ CONFIGURACIÓN GENERAL ================
// ======================================================
using API_asemp.Contextos;
using API_asemp.Datos;
using API_asemp.Seguridad;
using API_asemp.Servicios;
using API_asemp.Servicios.SAT;
using API_asemp.Utilerias;
using ASEMP.CFDI.DescargaMasiva;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Text;

// ===== IMPORTANTE PARA SERILOG =====
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//// ====================== SERILOG FILE LOGGING ======================
//var satPath = builder.Configuration["SatStoragePath"];
//var logsPath = Path.Combine(satPath!, "logs_jobs", "sat_log_.txt");

//// Crear carpeta logs si no existe
//Directory.CreateDirectory(Path.GetDirectoryName(logsPath)!);

//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Information()
//    .WriteTo.File(
//        path: logsPath,
//        rollingInterval: RollingInterval.Day,
//        shared: true,
//        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
//    )
//    .CreateLogger();

//builder.Host.UseSerilog();


// ====================== SERILOG FILE LOGGING ======================
var configuredSatPath = builder.Configuration["SatStoragePath"];

// Fallback seguro para hosting (carpeta del sitio)
var fallbackSatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads", "SAT");

// Si no hay config o apunta a C:\ (hosting no permite), usa fallback
var satPath = string.IsNullOrWhiteSpace(configuredSatPath)
    ? fallbackSatPath
    : (configuredSatPath.StartsWith(@"C:\", StringComparison.OrdinalIgnoreCase)
        ? fallbackSatPath
        : configuredSatPath);

// Asegurar carpeta base
Directory.CreateDirectory(satPath);

var logsDir = Path.Combine(satPath, "logs_jobs");
Directory.CreateDirectory(logsDir);

var logsPath = Path.Combine(logsDir, "sat_log_.txt");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        path: logsPath,
        rollingInterval: RollingInterval.Day,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();




// ======================================================
// ============ CIFRADO DE DATOS SENSIBLES ===============
// ======================================================
builder.Services.AddDataProtection()
    .SetApplicationName("API_asemp")
    .PersistKeysToFileSystem(
        new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "Keys"))
    );

// ======================================================
// =============== SERVICIOS BÁSICOS DEL API =============
// ======================================================
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Asesoría Empresarial",
        Version = "v1",
        Description = "API con autenticación JWT para gestión de usuarios y documentos SAT"
    });

    // --- Configuración de autenticación JWT para Swagger ---
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Introduce el token JWT de la forma: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    };

    c.AddSecurityRequirement(securityRequirement);

    // ==========================================================
    // ✅ Soporte para archivos (IFormFile) y multipart/form-data
    // ==========================================================
    c.SupportNonNullableReferenceTypes();
    c.OperationFilter<FileUploadOperationFilter>(); // <-- Importante
});

// ======================================================
// =============== CONFIGURACIÓN BASE DE DATOS ===========
// ======================================================
builder.Services.AddDbContext<myDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("pg"))
           .UseSnakeCaseNamingConvention());

// ======================================================
// =============== CONFIGURACIÓN AUTENTICACIÓN JWT =======
// ======================================================
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtExpires = int.Parse(builder.Configuration["Jwt:ExpiresHours"] ?? "8");

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.RequireHttpsMetadata = false;
    opt.SaveToken = true;
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// ======================================================
// =============== CONFIGURACIÓN CORS (Angular) ==========
// ======================================================
var corsPolicy = "_allowAngular";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicy,
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// ======================================================
// =============== INYECCIÓN DE DEPENDENCIAS =============
// ======================================================
builder.Services.AddScoped<Departamentos>();
builder.Services.AddScoped<Roles>();
builder.Services.AddScoped<Usuarios>();
builder.Services.AddScoped<Clientes>();
builder.Services.AddScoped<Cobros_Clientes>();
builder.Services.AddScoped<CertificadosSAT>();
builder.Services.AddScoped<SolicitudesSAT>();
builder.Services.AddScoped<DescargasSAT>();
builder.Services.AddScoped<Acciones>();
builder.Services.AddScoped<Roles_Acciones>();
builder.Services.AddScoped<Catalogos>();

builder.Services.AddScoped<CertificadosService>();

builder.Services.AddScoped<VerificacionesSAT_Datos>();

builder.Services.AddScoped<ITableroFiscalService, TableroFiscalService>();


// ======== REPORTES DE COBROS (OBLIGATORIO) ========
builder.Services.AddScoped<ReportesCobrosService>();
builder.Services.AddScoped<ReportesCobrosArchivoService>();




// ======================================================
// =============== INTEGRACIÓN CON LIBRERÍA SAT ==========
// ======================================================
builder.Services.AddCfdiDescargaMasivaServices(); // Registro de la DLL ASEMP
builder.Services.AddScoped<SatService>(); // Tu envoltorio de negocio SAT







/* ======== Descarga Automática de CFDI's ======== */
builder.Services.AddScoped<SatJobService>();
builder.Services.AddScoped<SatJobOrchestrator>();

// HostedService automático REAL que ejecuta las solicitudes/verificaciones
builder.Services.AddHostedService<SatJobsBackgroundService>();



// ======================================================
// =================== CONSTRUCCIÓN APP =================
// ======================================================
var app = builder.Build();




// ======================================================
// ========== GENERACIÓN AUTOMÁTICA INICIO DE AÑO ========
// ======================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<myDbContext>();
    var cobros = scope.ServiceProvider.GetRequiredService<Cobros_Clientes>();

    // EJECUTAR SOLO EN ENERO
    if (DateTime.Now.Month == 1)
    {
        var clientes = db.clientes
            .Where(c => c.estatus == true)
            .Select(c => c.id)
            .ToList();

        foreach (var id in clientes)
            cobros.GenerateCobrosAnuales(id, DateTime.Now.Year);
    }
}







// ======================================================
// ==================== PIPELINE HTTP ====================
// ======================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API_asemp v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors(corsPolicy);
app.UseAuthentication();
app.UseAuthorization();

//este es de la seguridad de los roles
app.UseMiddleware<AccionesMiddleware>();



// ======================================================
// =================== RUTAS DE CONTROLADORES ============
// ======================================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ======================================================
// ======= HASH AUTOMÁTICO DE CONTRASEÑAS (SEED) =========
// ======================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<myDbContext>();

    var porHashear = db.usuarios
        .AsEnumerable()
        .Where(u => !API_asemp.Seguridad.PasswordHelper.IsHashed(u.contrasena))
        .ToList();

    foreach (var u in porHashear)
    {
        u.contrasena = API_asemp.Seguridad.PasswordHelper.Hash(u.contrasena ?? "123456");
    }

    if (porHashear.Count > 0)
        db.SaveChanges();
}

// ======================================================
// ===================== EJECUCIÓN APP ===================
// ======================================================
app.UseDeveloperExceptionPage();
app.Run();
