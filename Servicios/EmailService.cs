using System.Net;
using System.Net.Mail;

namespace API_asemp.Servicios
{
    // ======================================================
    // 🔹 Contrato para el envío de correos.
    //     Permite cambiar de proveedor (SMTP, SendGrid, etc.)
    //     sin tocar el código que lo consume.
    // ======================================================
    public interface IEmailService
    {
        Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml);

        // Envía un correo con uno o más archivos adjuntos (ej. un .zip con .cer/.key/.pfx)
        Task EnviarConAdjuntosAsync(string destinatario, string asunto, string cuerpoHtml, IEnumerable<EmailAttachment> adjuntos);
    }

    // ======================================================
    // 🔹 Representa un archivo a adjuntar en un correo.
    // ======================================================
    public class EmailAttachment
    {
        public string NombreArchivo { get; }
        public byte[] Contenido { get; }
        public string ContentType { get; }

        public EmailAttachment(string nombreArchivo, byte[] contenido, string contentType = "application/octet-stream")
        {
            NombreArchivo = nombreArchivo;
            Contenido = contenido;
            ContentType = contentType;
        }
    }

    // ======================================================
    // 🔹 Implementación vía SMTP usando la configuración
    //     de la sección "Smtp" en appsettings.json
    // ======================================================
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
            => EnviarConAdjuntosAsync(destinatario, asunto, cuerpoHtml, Array.Empty<EmailAttachment>());

        public async Task EnviarConAdjuntosAsync(string destinatario, string asunto, string cuerpoHtml, IEnumerable<EmailAttachment> adjuntos)
        {
            if (string.IsNullOrWhiteSpace(destinatario))
            {
                Console.WriteLine("⚠️ No se envió correo: destinatario vacío.");
                return;
            }

            var host = _config["Smtp:Host"];
            var portStr = _config["Smtp:Port"];
            var user = _config["Smtp:User"];
            var password = _config["Smtp:Password"];
            var enableSslStr = _config["Smtp:EnableSsl"];
            var fromEmail = _config["Smtp:From"];
            var fromName = _config["Smtp:FromName"] ?? "ASEMP";

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user))
            {
                Console.WriteLine("⚠️ SMTP no está configurado (Smtp:Host / Smtp:User). Se omite el envío de correo.");
                return;
            }

            int port = int.TryParse(portStr, out var p) ? p : 587;
            bool enableSsl = !bool.TryParse(enableSslStr, out var ssl) || ssl; // true por defecto

            using var mensaje = new MailMessage
            {
                From = new MailAddress(string.IsNullOrWhiteSpace(fromEmail) ? user! : fromEmail, fromName),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };
            mensaje.To.Add(destinatario);

            // Los MemoryStream de los adjuntos deben permanecer vivos hasta
            // que el correo se envíe, así que los liberamos al final (finally).
            var streamsAdjuntos = new List<MemoryStream>();

            try
            {
                foreach (var adj in adjuntos)
                {
                    var ms = new MemoryStream(adj.Contenido);
                    streamsAdjuntos.Add(ms);
                    mensaje.Attachments.Add(new Attachment(ms, adj.NombreArchivo, adj.ContentType));
                }

                using var cliente = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(user, password),
                    EnableSsl = enableSsl
                };

                await cliente.SendMailAsync(mensaje);
                Console.WriteLine($"✅ Correo enviado correctamente a {destinatario}");
            }
            catch (Exception ex)
            {
                // No se relanza la excepción: un fallo de correo no debe
                // tumbar el flujo de negocio (ej. creación de usuario).
                Console.WriteLine($"❌ Error enviando correo a {destinatario}: {ex.Message}");
            }
            finally
            {
                foreach (var ms in streamsAdjuntos) ms.Dispose();
            }
        }
    }
}
