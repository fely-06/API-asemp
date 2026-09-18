using API_asemp.Contextos;
using API_asemp.Models;
using API_asemp.Models.BD;
using API_asemp.Utilerias;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using System.Security.Cryptography.X509Certificates;

namespace API_asemp.Servicios
{
    public class CertificadosService
    {
        private readonly myDbContext _db;
        private readonly IConfiguration _config;

        public CertificadosService(myDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public void GuardarCambios() => _db.SaveChanges();

        // ==========================================================
        // 🔹 Guardar archivo individual
        // ==========================================================
        public async Task<string> GuardarArchivo(IFormFile archivo, string tipo, int clienteId, string rfc)
        {
            if (archivo == null || archivo.Length == 0)
                throw new ArgumentException("Archivo vacío o nulo.");

            string carpetaCliente = ArchivosHelper.CarpetaCliente(_config, clienteId, rfc);
            string nombreArchivo = tipo.ToLower() switch
            {
                "cer" => "certificado.cer",
                "key" => "llave.key",
                "pfx" => "certificado.pfx",
                _ => archivo.FileName
            };

            string rutaArchivo = Path.Combine(carpetaCliente, nombreArchivo);

            using (var fs = new FileStream(rutaArchivo, FileMode.Create, FileAccess.Write))
                await archivo.CopyToAsync(fs);

            return Path.GetRelativePath(_config["SatStoragePath"]!, rutaArchivo).Replace("\\", "/");
            //return Path.GetRelativePath(ArchivosHelper.GetBasePath(), rutaArchivo).Replace("\\", "/");
        }

        // ==========================================================
        // 🔹 Crear o actualizar cliente con certificados
        // ==========================================================
        public async Task<(bool Ok, string Mensaje, object? Datos)> RegistrarClienteYCertificado(ClienteCertDto dto)
        {
            try
            {
                // Buscar si ya existe el cliente (modo edición)
                var cliente = _db.clientes.FirstOrDefault(c => c.id == dto.id);

                if (cliente == null)
                {
                    // Crear nuevo cliente
                    cliente = new Cliente
                    {
                        usuario_id = dto.usuario_id,
                        razon_social = dto.razon_social,
                        telefono = dto.telefono,
                        correo_electronico = dto.correo_electronico,
                        direccion = dto.direccion,
                        honorarios_subtotal = dto.honorarios_subtotal,
                        fecha_registro = DateTime.UtcNow,
                        estatus = dto.estatus,
                        // 🔥 Retenciones
                        usa_retenciones = dto.usa_retenciones,
                        porc_isr_ret = dto.porc_isr_ret ?? 0,
                        porc_iva_ret = dto.porc_iva_ret ?? 0

                    };
                    _db.clientes.Add(cliente);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    // Actualizar datos existentes
                    cliente.usuario_id = dto.usuario_id != 0 ? dto.usuario_id : cliente.usuario_id;
                    cliente.razon_social = dto.razon_social ?? cliente.razon_social;
                    cliente.telefono = dto.telefono ?? cliente.telefono;
                    cliente.correo_electronico = dto.correo_electronico ?? cliente.correo_electronico;
                    cliente.direccion = dto.direccion ?? cliente.direccion;
                    cliente.honorarios_subtotal = dto.honorarios_subtotal != 0 ? dto.honorarios_subtotal : cliente.honorarios_subtotal;
                    cliente.estatus = dto.estatus;
                    await _db.SaveChangesAsync();
                }

                // ======= Archivos =======
                string carpetaCliente = ArchivosHelper.CarpetaCliente(_config, cliente.id, dto.rfc ?? "SIN_RFC");
                string cerPath = Path.Combine(carpetaCliente, "certificado.cer");
                string keyPath = Path.Combine(carpetaCliente, "llave.key");
                string pfxPath = Path.Combine(carpetaCliente, "certificado.pfx");

                X509Certificate2? xcert = null;

                // ======= Si hay archivos nuevos, procesar =======
                if (dto.cer != null && dto.key != null)
                {
                    string[] permitidasCer = { ".cer" };
                    string[] permitidasKey = { ".key" };

                    string extCer = Path.GetExtension(dto.cer.FileName).ToLowerInvariant();
                    string extKey = Path.GetExtension(dto.key.FileName).ToLowerInvariant();

                    if (!permitidasCer.Contains(extCer) || !permitidasKey.Contains(extKey))
                        return (false, "Extensiones inválidas. Solo se permiten .cer y .key.", null);

                    if (dto.cer.Length > 2_000_000 || dto.key.Length > 2_000_000)
                        return (false, "Los archivos exceden 2MB.", null);

                    // Guardar archivos físicamente
                    using (var f = System.IO.File.Create(cerPath)) await dto.cer.CopyToAsync(f);
                    using (var f = System.IO.File.Create(keyPath)) await dto.key.CopyToAsync(f);

                    // Validar y generar PFX
                    var cerBytes = await File.ReadAllBytesAsync(cerPath);
                    var keyBytes = await File.ReadAllBytesAsync(keyPath);

                    var certParser = new Org.BouncyCastle.X509.X509CertificateParser();
                    var bcCert = certParser.ReadCertificate(cerBytes);

                    Org.BouncyCastle.Crypto.AsymmetricKeyParameter privateKey;
                    try
                    {
                        privateKey = PrivateKeyFactory.DecryptKey(dto.contrasena.Trim().ToCharArray(), keyBytes);
                    }
                    catch
                    {
                        try
                        {
                            privateKey = PrivateKeyFactory.CreateKey(keyBytes);
                        }
                        catch (Exception ex2)
                        {
                            return (false, $"Error al validar la llave privada o contraseña: {ex2.Message}", null);
                        }
                    }

                    var keyPair = new Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair(bcCert.GetPublicKey(), privateKey);
                    var rsaPrivate = DotNetUtilities.ToRSA((Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters)keyPair.Private);
                    var certWithKey = new X509Certificate2(bcCert.GetEncoded());
                    var finalCert = certWithKey.CopyWithPrivateKey(rsaPrivate);

                    var pfxBytes = finalCert.Export(X509ContentType.Pfx, dto.contrasena);
                    await File.WriteAllBytesAsync(pfxPath, pfxBytes);
                    xcert = new X509Certificate2(pfxBytes, dto.contrasena, X509KeyStorageFlags.EphemeralKeySet);
                }
                else
                {
                    Console.WriteLine("⚠️ No se enviaron archivos nuevos, se conservan los anteriores.");
                }

                // ======= RFC y fechas =======
                string rfcExtraido = xcert != null ? (RfcHelper.ExtraerRFC(xcert.RawData) ?? "SIN_RFC") : dto.rfc ?? "SIN_RFC";

                // Obtener o crear certificado SAT
                var certSat = _db.certificados_sat.FirstOrDefault(x => x.cliente_id == cliente.id);
                if (certSat == null)
                {
                    certSat = new CertificadoSAT
                    {
                        cliente_id = cliente.id,
                        fecha_registro = DateTime.UtcNow,
                        estatus = true
                    };
                    _db.certificados_sat.Add(certSat);
                }

                if (dto.cer != null)
                    certSat.archivo_cer = Path.GetRelativePath(_config["SatStoragePath"]!, cerPath);
                if (dto.key != null)
                    certSat.archivo_key = Path.GetRelativePath(_config["SatStoragePath"]!, keyPath);
                if (xcert != null)
                    certSat.archivo_pfx = Path.GetRelativePath(_config["SatStoragePath"]!, pfxPath);

                certSat.rfc = rfcExtraido;
                certSat.contrasena = dto.contrasena ?? certSat.contrasena;
                certSat.fecha_vigencia_inicio = xcert?.NotBefore.ToUniversalTime() ?? certSat.fecha_vigencia_inicio;
                certSat.fecha_vigencia_fin = xcert?.NotAfter.ToUniversalTime() ?? certSat.fecha_vigencia_fin;


                await _db.SaveChangesAsync();

                // ======= Respuesta =======
                return (true, "Cliente guardado correctamente.", new
                {
                    mensaje = "Cliente y certificado actualizados correctamente.",
                    cliente_id = cliente.id,
                    cliente.razon_social,
                    certificado = new
                    {
                        rfc = certSat.rfc,
                        fecha_inicio = certSat.fecha_vigencia_inicio?.ToString("yyyy-MM-dd"),
                        fecha_fin = certSat.fecha_vigencia_fin?.ToString("yyyy-MM-dd"),
                        archivo_pfx = Path.GetFileName(certSat.archivo_pfx ?? "")
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("========== ERROR en RegistrarClienteYCertificado ==========");
                Console.WriteLine("Mensaje: " + ex.Message);

                if (ex.InnerException != null)
                {
                    Console.WriteLine("INNER: " + ex.InnerException.Message);

                    if (ex.InnerException.InnerException != null)
                        Console.WriteLine("INNER 2: " + ex.InnerException.InnerException.Message);
                }

                return (false, $"Error general: {ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message}", null);
            }


        }
    }
}